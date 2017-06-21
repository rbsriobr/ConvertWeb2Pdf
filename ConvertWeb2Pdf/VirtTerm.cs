#region Imports (5)

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;

#endregion Imports (5)

namespace ConvertWeb2Pdf
{
	public class VirtTerm
	{
		#region Enums of VirtTerm (3)

		public enum BkgColors
		{
			//Extended = 48,
			Black = 40, Blue = 44, BoldBright = 1, BrightBlack = 100,

			BrightBlue = 104, BrightCyan = 106, BrightGreen = 102, BrightMagenta = 105,
			BrightRed = 101, BrightWhite = 107, BrightYellow = 103, Cyan = 46,
			Default = 49, Green = 42, Magenta = 45, Negative = 7,
			NoUnderline = 24, Positive = 27, Red = 41, Underline = 4,
			White = 47, Yellow = 43
		}

		public enum ForColors
		{
			//Extended    = 38,
			Black = 30, Red = 31, Green = 32, Yellow = 33,

			Blue = 34, Magenta = 35, Cyan = 36, White = 37,
			BoldBright = 1, Underline = 4, NoUnderline = 24, BrightCyan = 96,
			BrightWhite = 97, BrightGreen = 92, BrightYellow = 93, BrightBlue = 94,
			BrightMagenta = 95, Default = 39, BrightBlack = 90, BrightRed = 91,
			Negative = 7, Positive = 27,
		}

		[Flags]
		public enum supportedOSs { None, Vista, Windows7, Windows8, Windows81, Windows10 };

		#endregion Enums of VirtTerm (3)

		#region Members of VirtTerm (10)

		public int osversionbuild                            = 0;
		public int osversionmajor                            = 0;
		public int osversionminor                            = 0;
		private const int ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
		private const int STD_INPUT_HANDLE                   = -10;
		private const int STD_OUTPUT_HANDLE                  = -11;
		private bool CanEnableVirtualTerminal                = false;
		private IntPtr handle                                = IntPtr.Zero;
		private bool initiated                               = false;
		static private object critical                       = new object ( );
		private string lasttext                              = "";

		#endregion Members of VirtTerm (10)

		#region Constructors of VirtTerm (1)

		public VirtTerm ( )
		{
		}

		#endregion Constructors of VirtTerm (1)

		#region Methods of VirtTerm (8)

		[DllImport ("kernel32.dll")]
		private static extern bool GetConsoleMode (IntPtr hConsoleHandle, out uint lpMode);

		[DllImport ("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetStdHandle (int nStdHandle);

		[DllImport ("kernel32.dll")]
		private static extern bool SetConsoleMode (IntPtr hConsoleHandle, uint dwMode);

		public String GetSubstring (Char startChar, Char endChar, String text, ref int offset)
		{
			int ed, st;
			if ((st = text.IndexOf (startChar, offset)) >= 0)
			{
				offset = st;
				if ((ed = text.IndexOf (endChar, st)) >= 0)
				{
					if (st > 0)
					{
						if (text[st - 1] == startChar)
						{
							return null;
						}
						return text.Substring (st, ed - st + 1);
					}
				}
			}
			return null;
		}

		private void Init ( )
		{
			if (!initiated)
			{
				initiated = true;
				uint mode;
				handle = GetStdHandle (STD_OUTPUT_HANDLE);
				GetConsoleMode (handle, out mode);
				mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
				SetConsoleMode (handle, mode);

				var osver = System.Environment.OSVersion;
				osversionmajor = osver.Version.Major;
				osversionminor = osver.Version.Minor;
				osversionbuild = osver.Version.Build;

				supportedOSs OSs = ManifestSupportedOSs ( );

				if (!OSs.HasFlag (supportedOSs.Windows10))
				{
					Console.WriteLine ("<supportedOS Id=\"{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}\" /> not found in manifest");
					Console.WriteLine ("OSVersion can not be checked then the virtual terminal will be disabled");
				}
				else
				{
					if (osversionmajor >= 10 && osversionminor > 0)
					{
						CanEnableVirtualTerminal = true;
						return;
					}
					else if (osversionmajor >= 10 && osversionbuild >= 10586)
					{
						CanEnableVirtualTerminal = true;
						return;
					}
					else
					{
						Console.WriteLine ("Virtual terminal escape sequences are only available in Windows 10 Build 10586 or newer ");
						Console.WriteLine ("Virtual terminal will be disabled");
					}
				}
				CanEnableVirtualTerminal = false;
			}
		}

		static public supportedOSs ManifestSupportedOSs ( )
		{
			supportedOSs OSs = supportedOSs.None;
			Assembly asm = Assembly.ReflectionOnlyLoad (Assembly.GetCallingAssembly ( ).FullName);

			string containingFileName = asm.GetName ( ).Name + ".app.manifest";

			XmlDocument doc = new XmlDocument ( );
			using (Stream stream = asm.GetManifestResourceStream (containingFileName))
			{
				doc.Load (stream);

				XmlNodeList elem = doc.GetElementsByTagName ("supportedOS");
				foreach (XmlLinkedNode node in elem)
				{
					foreach (XmlNode attr in node.Attributes)
					{
						string value = attr.Value.Replace (" ", "").Replace ("-", "");
						if (value.Contains ("e2011457-1546-43c5-a5fe-008deee3d3f0"))
						{
							OSs |= supportedOSs.Vista;
						}
						else if (value.Contains ("35138b9a-5d96-4fbd-8e2d-a2440225f93a"))
						{
							OSs |= supportedOSs.Windows7;
						}
						else if (value.Contains ("4a2f28e3-53b9-4441-ba9c-d69d4a4a6e38"))
						{
							OSs |= supportedOSs.Windows8;
						}
						else if (value.Contains ("1f676c76-80e1-4239-95bb-83d0f6d0da78"))
						{
							OSs |= supportedOSs.Windows81;
						}
						else if (value.Contains ("8e0f7a12bfb34fe8b9a548fd50a15a9a"))
						{
							OSs |= supportedOSs.Windows10;
						}
					}
				}
			}
			return OSs;
		}

		public void PrintText (string text, bool repeat = true)
		{
			lock (critical)
			{
				Init ( );
				if (repeat || lasttext != text)
				{
					lasttext = text;
					Console.Write (Translate (text));
				}
			}
		}

		public string Translate (string text)
		{
			int offset = 0;
			string seqstr;
			ForColors forcolor = ForColors.Default;
			BkgColors bkgcolor = BkgColors.Default;

			while ((seqstr = GetSubstring ('<', '>', text, ref offset)) != null)
			{
				if (seqstr.Replace (" ", "") == "</>")
				{
					text = text.Replace (seqstr, "\x1b[0m");
					continue;
				}

				string [] ar = seqstr.Replace ("<", "").Replace (">", "").Replace (" ", "").Split (',');

				bool repl = true;

				for (int i = 0; i < ar.Length; i++)
				{
					string newelem = ar[i].Replace (",", "");

					if (!string.IsNullOrEmpty (newelem))
					{
						if (i == 0)
						{
							try
							{
								forcolor = (ForColors)System.Enum.Parse (typeof (ForColors), newelem, true);
							}
							catch (ArgumentException)
							{
								repl = false;
							}
						}
						else if (i == 1)
						{
							try
							{
								bkgcolor = (BkgColors)System.Enum.Parse (typeof (BkgColors), newelem, true);
							}
							catch (ArgumentException)
							{
								repl = false;
							}
						}
					}
				}
				string esc = "\x1b[" + ((int)forcolor).ToString ( ) + ";" + ((int)bkgcolor).ToString ( ) + "m";
				if (repl)
				{
					text = text.Replace (seqstr, CanEnableVirtualTerminal ? esc : "");
					offset += CanEnableVirtualTerminal ? esc.Length : 0;
				}
				else
				{
					offset += seqstr.Length;
				}
			}
			return CanEnableVirtualTerminal ? text + "\x1b[0m" : text;
		}

		#endregion Methods of VirtTerm (8)
	}
}