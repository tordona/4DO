using System;
using System.Diagnostics;
using System.IO;
using FourDO.Utilities;
using FourDO.Utilities.CueSharp;

namespace FourDO.Emulation.GameSource
{
	internal class FileGameSource : GameSourceBase
	{
		private const string LOG_PREFIX = "GameSource - ";

		private static readonly byte[] CdSyncPattern = new byte[] {
			0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00
		};

		private object _accessSemaphore = new object();

		private DataType imageDataType;
		private int sectorStride = 2048;
		private int sectorHeaderOffset = 0;
		private BinaryReader gameRomReader = null;

		public FileGameSource(string gameFilePath)
		{
			this.GameFilePath = gameFilePath;
		}

		public string GameFilePath { get; private set; }

		#region IGameSource Implementation

		protected override void OnOpen()
		{
			try
			{
				var identifiedFile = false;

				////////////////////
				// We will identify the correct format of this file if there is a matching cue file.
				// (Also, if they opened a cue file, we open the cue file here).
				string cueFile = Path.ChangeExtension(this.GameFilePath, ".cue");
				if (File.Exists(cueFile))
				{
					var cueSheet = new CueSheet(cueFile);
					if (cueSheet.Tracks == null || cueSheet.Tracks.Length < 1)
					{
						Trace.WriteLine(LOG_PREFIX + "Cue file found, but no tracks were defined within.");
					}
					else
					{
						var track = cueSheet.Tracks[0];

						bool isSupportedType = (track.TrackDataType == DataType.MODE1_2048 ||
						                        track.TrackDataType == DataType.MODE1_2352 ||
						                        track.TrackDataType == DataType.MODE2_2352 ||
						                        track.TrackDataType == DataType.CDI_2352);

						if (string.IsNullOrEmpty(track.DataFile.Filename) || track.DataFile.Filetype != FileType.BINARY || !isSupportedType)
						{
							Trace.WriteLine(LOG_PREFIX + "Cue file found, but the track within was not in a supported format: " + track.TrackDataType);
						}
						else
						{
							// Combine this track's filename with the cue file's directory
							string dir = Path.GetDirectoryName(this.GameFilePath) ?? "";
							string fileToOpen = Path.Combine(dir, track.DataFile.Filename);
							if (!File.Exists(fileToOpen))
							{
								// Fallback: in case the CUE file stored a full path from another machine
								fileToOpen = Path.Combine(dir, Path.GetFileName(track.DataFile.Filename));
							}

							try
							{
								if (File.Exists(fileToOpen))
								{
									this.gameRomReader = new BinaryReader(new FileStream(fileToOpen, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
									this.imageDataType = track.TrackDataType;

									if (track.TrackDataType == DataType.MODE1_2048)
									{
										this.sectorStride = 2048;
										this.sectorHeaderOffset = 0;
									}
									else if (track.TrackDataType == DataType.MODE1_2352)
									{
										this.sectorStride = 2352;
										this.sectorHeaderOffset = 0x10;
									}
									else // MODE2_2352 or CDI_2352
									{
										this.sectorStride = 2352;
										this.sectorHeaderOffset = 0x18;
									}

									// If raw 2352, dynamically verify sync header
									DetectSyncHeaderMode();

									identifiedFile = true;
								}
								else
								{
									Trace.WriteLine(LOG_PREFIX + "Cue file referenced a target binary file that could not be found: " + fileToOpen);
								}
							}
							catch (Exception ex)
							{
								Trace.WriteLine(LOG_PREFIX + "Cue file found, but there was an error opening the file it indicated within. Problem encountered was: " + ex.ToString());
							}
						}
					}
				}

				///////////
				// If we still aren't done, then the CUE file route did not work out.
				if (!identifiedFile)
				{
					// If the user specified a .cue file, but we couldn't resolve its binary track, do NOT open the .cue text file as a CD!
					if (string.Equals(Path.GetExtension(this.GameFilePath), ".cue", StringComparison.OrdinalIgnoreCase))
					{
						throw new FileNotFoundException("Could not find or identify a valid binary data track for CUE file: " + this.GameFilePath);
					}

					var fileInfo = new FileInfo(this.GameFilePath);
					if (!fileInfo.Exists)
					{
						throw new FileNotFoundException("Game ROM file does not exist: " + this.GameFilePath);
					}

					// Try to guess the encoding by looking at the file extension and size.
					if (string.Equals(fileInfo.Extension, ".ISO", StringComparison.OrdinalIgnoreCase))
					{
						// ISO will typically be 2048.
						this.imageDataType = DataType.MODE1_2048;
						this.sectorStride = 2048;
						this.sectorHeaderOffset = 0;

						if (fileInfo.Length % 2048 != 0 && fileInfo.Length % 2352 == 0)
						{
							this.imageDataType = DataType.MODE1_2352;
							this.sectorStride = 2352;
							this.sectorHeaderOffset = 0x10;
						}
					}
					else
					{
						// Other types (like BIN) we'll assume 2352.
						this.imageDataType = DataType.MODE1_2352;
						this.sectorStride = 2352;
						this.sectorHeaderOffset = 0x10;

						if (fileInfo.Length % 2352 != 0 && fileInfo.Length % 2048 == 0)
						{
							this.imageDataType = DataType.MODE1_2048;
							this.sectorStride = 2048;
							this.sectorHeaderOffset = 0;
						}
					}

					Trace.WriteLine(LOG_PREFIX + "No cue file used. Encoding format determined: " + this.imageDataType.ToString());

					this.gameRomReader = new BinaryReader(new FileStream(this.GameFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

					DetectSyncHeaderMode();
				}
			}
			catch
			{
				throw;
			}
		}

		private void DetectSyncHeaderMode()
		{
			if (this.gameRomReader != null && this.sectorStride == 2352 && this.gameRomReader.BaseStream.Length >= 2352)
			{
				long savedPos = this.gameRomReader.BaseStream.Position;
				try
				{
					this.gameRomReader.BaseStream.Position = 0;
					byte[] headerBytes = this.gameRomReader.ReadBytes(16);
					if (headerBytes != null && headerBytes.Length >= 16 && HasCdSyncPattern(headerBytes))
					{
						byte modeByte = headerBytes[15];
						if (modeByte == 0x01)
							this.sectorHeaderOffset = 0x10;
						else if (modeByte == 0x02)
							this.sectorHeaderOffset = 0x18;
					}
				}
				finally
				{
					this.gameRomReader.BaseStream.Position = savedPos;
				}
			}
		}

		private static bool HasCdSyncPattern(byte[] header)
		{
			if (header == null || header.Length < 12)
				return false;
			if (header[0] != 0x00 || header[11] != 0x00)
				return false;
			for (int i = 1; i <= 10; i++)
			{
				if (header[i] != 0xFF)
					return false;
			}
			return true;
		}

		protected override void OnClose()
		{
			lock (_accessSemaphore)
			{
				if (this.gameRomReader != null)
				{
					this.gameRomReader.Close();
					this.gameRomReader = null;
				}
			}
		}

		protected override void OnReadSector(IntPtr destinationBuffer, int sectorNumber)
		{
			if (destinationBuffer == IntPtr.Zero)
				return;

			lock (_accessSemaphore)
			{
				if (this.gameRomReader == null)
					return; // No game loaded.

				long targetPosition = (long)this.sectorStride * sectorNumber + this.sectorHeaderOffset;
				byte[] bytesToCopy = null;

				if (targetPosition >= 0 && targetPosition < this.gameRomReader.BaseStream.Length)
				{
					this.gameRomReader.BaseStream.Position = targetPosition;
					bytesToCopy = this.gameRomReader.ReadBytes(2048);
				}

				int bytesRead = (bytesToCopy != null) ? bytesToCopy.Length : 0;

				unsafe
				{
					byte* dest = (byte*)destinationBuffer.ToPointer();
					if (bytesRead > 0)
					{
						fixed (byte* sourceRomBytesPointer = bytesToCopy)
						{
							Memory.CopyMemory(destinationBuffer, new IntPtr(sourceRomBytesPointer), bytesRead);
						}
					}

					for (int i = bytesRead; i < 2048; i++)
					{
						dest[i] = 0;
					}
				}
			}
		}

		protected override int GetDefaultSectorCount()
		{
			lock (_accessSemaphore)
			{
				if (this.gameRomReader != null && this.sectorStride > 0)
				{
					try
					{
						return (int)(this.gameRomReader.BaseStream.Length / this.sectorStride);
					}
					catch { }
				}
				return 0;
			}
		}

		#endregion // IGameSource Implementation
	}
}
