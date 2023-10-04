﻿
/*
	I wrote this thing 4 years ago
	yes, there are goto statements in c#
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;

public class MetadataReader {

	// ---- Classes ----

	public enum TagLibraries {
		/// This file doesn't have any tags.
		None = 0,
		/// An error was encountered while reading tags.
		Error = -1,
		/// This file have a ID3v1 tag.
		ID3Ver1 = 31,
		/// This file have a ID3v2.2 tag.
		ID3Ver2Point2 = 32,
		/// This file have a ID3v2.3 tag.
		ID3Ver2Point3 = 33,
		/// This file have a ID3v2.4 tag.
		ID3Ver2Point4 = 34,
	}

	public class PopularimeterClass {
		public string Email = "";
		public byte Value = 0;
		public byte[] Counters = new byte[0];

		public PopularimeterClass (string email, byte value, byte[] counters) {
			Email = email;
			Value = value;
			Counters = counters;
		}
		public PopularimeterClass () {
		}
	}

	// ---- Embed tags ----

	// Obsolete

	/// Returns the audio length of the file in seconds.
	[Obsolete("nah I think you can calculate it yourself. (Use 'LengthInMillisecond' to get the length in millisecond.)", true)]
	public float LengthInSeconds;
	/// Returns the attached image as Texture2D if have one, else returns a white 1x1 image.
	[Obsolete("There can be multiple images in a single tag. Use 'AttachedTextures' for a list of texture images or 'ImageCover' for the cover.", true)]
	public readonly Texture2D AttachedImage = new Texture2D (1, 1);

	// Usable

	/// The audio's encoding. Returns a blank string if not declared.
	public readonly string EncodingOptions = "";
	/// The audio's title. Returns a blank string if not declared.
	public readonly string Title = "";
	/// The audio's subtitle. Returns a blank string if not declared.
	public readonly string Subtitle = "";
	/// The audio's artist/author. Returns a blank string if not declared.
	public readonly string Artist = "";
	/// The artist's URL link. Returns a blank string if not declared.
	public readonly string ArtistURL = "";
	/// The audio's performer/conductor. Returns a blank string if not declared.
	public readonly string Performer = "";
	/// The audio's composer. Returns a blank string if not declared.
	public readonly string Composer = "";
	/// The audio's type/genre as string. Returns a blank string if not declared.
	public readonly string Type = "";
	/// The audio album's name. Returns a blank string if not declared.
	public readonly string Album = "";
	/// The track position of the audio file as string. Returns a blank string if not declared.
	public readonly string Track = "";
	/// The audio album's artist. Returns a blank string if not declared.
	public readonly string AlbumAuthor = "";
	/// The file publisher. Returns a blank string if not declared.
	public readonly string Publisher = "";
	/// Copyright message attached to the file. Returns a blank string if not declared.
	public readonly string Copyright = "";
	/// The recording code attached to the file. Returns a blank string if not declared.
	public readonly string RecordingCode = "";
	/// Comment attached to the file. Returns a blank string if not declared.
	public readonly string Comments = "";
	/// Other field attached to the file. Returns a blank string if not declared.
	public readonly string Other = "";
	/// The track position of the audio file as integer. Returns -1 if not declared.
	public readonly int TrackInt = -1;
	/// Returns the recorded year of the audio if declared, else returns -1.
	public readonly int RecordedYear = -1;
	/// The date that the file has been encoded.
	public readonly DateTime EncodedDate = new DateTime();
	/// Returns the BPM rate of the audio file if declared, else returns -1.
	public readonly int BeatsPerMinute = -1;
	/// Returns the audio length of the file in milliseconds.
	public readonly int LengthInMilliseconds = 0;
	/// The file's tag library.
	public readonly TagLibraries TagLibrary = TagLibraries.None;
		

	// ---- Reading tags ----

	/// Generates an audio metadata list using the file from the given path.
	public MetadataReader(string path, bool debugMode) {
		if (!File.Exists (path))
			throw new ArgumentException ("Path does not exist.");
		// ready to read tags
		FileStream stream = new FileStream (path, FileMode.Open);
		BinaryReader reader = new BinaryReader(stream);
		try {
			// read ID3v1 tags
			byte[] sample = new byte[128];
			stream.Seek(-128, SeekOrigin.End);
			stream.Read(sample, 0, 128);
			string textToGet = System.Text.Encoding.Default.GetString(sample, 0, 3);
			if (textToGet.CompareTo ("TAG") == 0) {
				TagLibrary = TagLibraries.ID3Ver1;
				Title = System.Text.Encoding.Default.GetString(sample, 3, 30);
				Artist = System.Text.Encoding.Default.GetString(sample, 33, 30);
				Album = System.Text.Encoding.Default.GetString(sample, 63, 30);
				try {
					RecordedYear = Convert.ToInt32(System.Text.Encoding.Default.GetString(sample, 93, 4));
				} catch (FormatException) {RecordedYear = -1;}
			}

			// read ID3v2 tags
			stream.Seek(0, SeekOrigin.Begin);
			byte[] tag = new byte[3];
			byte[] version = new byte[2];
			byte[] flag = new byte[1];
			byte[] length = new byte[4];
			reader.Read(tag, 0, tag.Length);
			reader.Read(version, 0, version.Length);
			reader.Read(flag, 0, flag.Length);
			reader.Read(length, 0, length.Length);
			ulong totalSize = (ulong)length [0] << 21 | (ulong)length [1] << 14 | (ulong)length [2] << 7 | (ulong)length [3];
			if (debugMode) Debug.Log(totalSize);
			ulong readedSize = 0;

			if (System.Text.Encoding.Default.GetString (tag) == "ID3" && (version [0] == 0x03 || version [0] == 0x04) && version [1] == 0x00) {
				if (version [0] == 0x03)
					TagLibrary = TagLibraries.ID3Ver2Point3;
				if (version [0] == 0x04)
					TagLibrary = TagLibraries.ID3Ver2Point4;

				byte[] id = new byte[4];
				byte[] size = new byte[4];
				byte[] frameFlag = new byte[2];
				startReading:
				reader.Read(id, 0, id.Length);
				reader.Read(size, 0, size.Length);
				reader.Read (frameFlag, 0, frameFlag.Length);
				ulong dataSize = (ulong)size [0] << 21 | (ulong)size [1] << 14 | (ulong)size [2] << 7 | (ulong)size [3];
				readedSize += dataSize + 10;
				byte[] body = new byte[dataSize];
				if (debugMode) Debug.Log(readedSize + " | " + dataSize + " | " + System.Text.Encoding.Default.GetString (id));
				reader.Read(body, 0, body.Length);
				if (true) {
					// "TSSE" = Encoding Options
					if (System.Text.Encoding.Default.GetString (id) == "TSSE" && body.Length > 1) {
						if (body [0] == 0x01) {
							EncodingOptions = System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim();
						} else {
							EncodingOptions = System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
						}
					}
					// "TENC" = Encoding Options
					else if (System.Text.Encoding.Default.GetString (id) == "TENC" && body.Length > 1) {
						if (body [0] == 0x01) {
							EncodingOptions = System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim();
						} else {
							EncodingOptions = System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
						}
					}
					// "TIT2" = Title
					else if (System.Text.Encoding.Default.GetString (id) == "TIT2" && body.Length > 1) {
						if (body [0] == 0x01) {
							Title = System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim();
						} else {
							Title = System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
						}
					}
					// "TPE1" = Artist
					else if (System.Text.Encoding.Default.GetString (id) == "TPE1" && body.Length > 1) {
						if (body [0] == 0x01) {
							Artist = System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim();
						} else {
							Artist = System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
						}
					}
					// "TCOM" = Composer
					else if (System.Text.Encoding.Default.GetString (id) == "TCOM" && body.Length > 1) {
						if (body [0] == 0x01) {
							Composer = System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim();
						} else {
							Composer = System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
						}
					}
					// "TPE3" = Performer;
					else if (System.Text.Encoding.Default.GetString (id) == "TPE3" && body.Length > 1) {
						if (body [0] == 0x01) {
							Performer = System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim();
						} else {
							Performer = System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
						}
					}
					// "TALB" = Album
					else if (System.Text.Encoding.Default.GetString (id) == "TALB" && body.Length > 1) {
						if (body [0] == 0x01) {
							Album = System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim();
						} else {
							Album = System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
						}
					}
					// "TPE2" = Album Author
					else if (System.Text.Encoding.Default.GetString (id) == "TPE2" && body.Length > 1) {
						if (body [0] == 0x01) {
							AlbumAuthor = System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim();
						} else {
							AlbumAuthor = System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
						}
					}
					// "TYER" = Recorded Year
					else if (System.Text.Encoding.Default.GetString (id) == "TYER" && body.Length > 1) {
						string TempString = "";
						if (body [0] == 0x01) {
							TempString = System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim();
						} else {
							TempString = System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
						}
						int.TryParse(TempString, out RecordedYear);
					}
					// "TDEN" = Date encoded
					else if (System.Text.Encoding.Default.GetString (id) == "TDEN" && body.Length > 1) {
						string TempString = "";
						if (body [0] == 0x01) {
							TempString = System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim();
						} else {
							TempString = System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
						}
						DateTime.TryParse(TempString, out EncodedDate);
					}
					// "TRCK" = Track
					else if (System.Text.Encoding.Default.GetString (id) == "TRCK" && body.Length > 1) {
						if (body [0] == 0x01) {
							Track = "#" + System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim();
						} else {
							Track = "#" + System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
						}
						if (Track.IndexOf('/') >= 0)
							Track = Track.Substring(0, Track.IndexOf('/'));
						TrackInt = Convert.ToInt32(Track.Remove(0, Track.IndexOf('#') + 1));
						Track = "#" + TrackInt;
					}
					// "TPUB" = Publisher
					else if (System.Text.Encoding.Default.GetString (id) == "TPUB" && body.Length > 1) {
						if (body [0] == 0x01) {
							Publisher = "#" + System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim();
						} else {
							Publisher = "#" + System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
						}
					}
					// "TBPM" = Beats per Minute
					else if (System.Text.Encoding.Default.GetString (id) == "TBPM" && body.Length > 1) {
						string TempString = "";
						if (body [0] == 0x01) {
							TempString = System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim();
						} else {
							TempString = System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
						}
						BeatsPerMinute = Convert.ToInt32(TempString);
					}
					// "TLEN" = Length
					else if (System.Text.Encoding.Default.GetString (id) == "TLEN" && body.Length > 1) {
						string TempString = "";
						if (body [0] == 0x01) {
							TempString = System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim();
						} else {
							TempString = System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
						}
						int.TryParse(TempString, out LengthInMilliseconds);
					}
					// "COMM" = Comment
					else if (System.Text.Encoding.Default.GetString (id) == "COMM" && body.Length > 1) {
						List<byte> newArray = new List<byte> (body);
						for (int a = 0; a<4; a++) {
							newArray = new List<byte> (body);
							newArray.RemoveAt (0);
							body = newArray.ToArray ();
						}
						if (body [0] == 0x01) {
							Comments += System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim() + ", ";
						} else {
							Comments += System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim() + ", ";
						}
					}
					// "TCON" = Type
					else if (System.Text.Encoding.Default.GetString (id) == "TCON" && body.Length > 1) {
						if (body [0] == 0x01) {
							Type = System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim();
						} else {
							Type = System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
						}
					}
					// "TCOP" = Copyright
					else if (System.Text.Encoding.Default.GetString (id) == "TCOP" && body.Length > 1) {
							if (body [0] == 0x01) {
							Copyright = System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim();
						} else {
							Copyright = System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
						}
					}
					if (readedSize < totalSize) goto startReading;
				}
			}
		} catch (Exception e) {
			Debug.LogWarning("WARNING: Throwing execption while reding tags may break things! Details: " + e.Message);
			TagLibrary = TagLibraries.Error;
		}
			
		// close reading
		doneReading:
		stream.Close();
		reader.Close();
		if (debugMode)
			Debug.Log (TagLibrary.ToString());
	}

	byte[] RemoveLast (byte[] array) {
		List<byte> newArray = new List<byte> (array);
		newArray.RemoveAt (0);
		return newArray.ToArray ();
	}
		
}
