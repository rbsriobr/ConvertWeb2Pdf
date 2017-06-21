#region Imports (7)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

#endregion Imports (7)

namespace ConvertWeb2Pdf
{
	internal class Workers
	{
		#region Enums of Workers (1)

		public enum _abortafter { no, yes, all };

		#endregion Enums of Workers (1)

		#region Members of Workers (20)

		public _abortafter abortafter    = _abortafter.no;
		public String curDir             = "";
		public ConsoleColor DefForeColor = ConsoleColor.Gray;
		public List<DocInfo> docInfoList = new List<DocInfo> ( );
		public Object [] elemArray;
		public String exeFileName        = "";
		public float fac                 = float.NaN;
		public int iDoc                  = 1;
		public int LastiDoc              = -1;
		public DateTime LastTimeDate     = DateTime.Now;
		public int maxtrials             = 3;
		public int nthread               = 0;
		public String options            = "";
		public int readsteps             = 0;
		public int timeout               = -1;
		public int totalbytessec         = 0;
		public VirtTerm vt               = new VirtTerm ( );
		private Object crit              = new Object ( );
		private Object printcritlock     = new Object ( );
		private Regex regEx              = new Regex ("<title>(.*?)</title>");

		#endregion Members of Workers (20)

		#region Methods of Workers (4)

		public void DoWork ( )
		{
			lock (crit)
			{
				Thread.CurrentThread.Name = "WorkThread" + nthread.ToString ( );
				nthread++;
			}

			WebClient wc = new WebClient ( );

			String Url  = null;
			int curDoc;

			while (true)
			{
				if (abortafter == _abortafter.all)
				{
					vt.PrintText ("\n\n <Yellow,>Thread aborted:</> " + Thread.CurrentThread.Name + "\n");
					return;
				}

				curDoc = iDoc++;

				if (curDoc >= docInfoList.Count)
				{
					iDoc = docInfoList.Count;
					break;
				}

				DocInfo docInfo = docInfoList.ElementAt (curDoc);

				int trial = 0;

				Print (iDoc, fac, 2);

				Url = docInfo.Url;

				if ((docInfo.DocTitle = GetTitle (ref wc, Url, ref totalbytessec, ref readsteps)) == null)
				{
					//Console.WriteLine ("\n Thread aborted: " + Thread.CurrentThread.Name);
					return;
				}

				bool printnotcreated = true;

				while (!docInfo.Exists && trial < maxtrials)
				{
					System.Diagnostics.Process pProcess = new System.Diagnostics.Process ( );

					//pProcess.ErrorDataReceived += pProcess_ErrorDataReceived;

					if (!File.Exists (exeFileName))
					{
						vt.PrintText (//"\n Thread aborted: " + Thread.CurrentThread.Name + ",
						"\n\n <Red,>File not found:</> " + exeFileName + "\n", false);
						return;
					}

					pProcess.StartInfo.CreateNoWindow = true;
					pProcess.StartInfo.UseShellExecute = false;
					pProcess.StartInfo.FileName = exeFileName;
					pProcess.StartInfo.WorkingDirectory = curDir;

					pProcess.StartInfo.Arguments = options + " \"" + Url + "\" \"" + docInfo.DocFileName + "\"";

					pProcess.Start ( );

					if (timeout >= 0)
					{
						bool state = pProcess.WaitForExit (timeout);
						if (!pProcess.HasExited)
						{
							pProcess.Kill ( );
						}
						if (!state)
						{
							printnotcreated = false;
							try
							{
								File.Delete (docInfo.DocFileName);
							}
							catch (System.IO.IOException)
							{ }
							vt.PrintText ("\n\n <Yellow,>Process killed:</> the file <" + docInfo.DocFileName + "> was not created from <" +
							Url + "> because it exeeded timeout after " + (trial + 1).ToString ( ) +
							" of " + maxtrials.ToString ( ) + " attempts.\n");
						}
					}
					else
					{
						pProcess.WaitForExit ( );
					}

					if (!docInfo.Exists)
					{
						trial++;
					}
				}
				if (!docInfo.Exists)
				{
					if (printnotcreated)
					{
						vt.PrintText ("\n\n <Red,>Document creation aborted:</> File <" + docInfo.DocFileName + "> was not created from URL <" + Url +
						"> after " + maxtrials.ToString ( ) + " of " + maxtrials.ToString ( ) + " attempts.\n");
						Print (iDoc, fac, 2);
					}

					if (abortafter == _abortafter.yes)
					{
						abortafter = _abortafter.all;
					}
				}
			}
		}

		private string GetTitle (ref WebClient wc, string url, ref int intspeed, ref int k)
		{
			String title = "";

			int i = 0;

			string Text = "";
			int len = 256;
			byte[] bytes = new byte[len];

			while (true)
			{
				try
				{
					DateTime dt1 = DateTime.Now;
					Stream stream = wc.OpenRead (url);
					int nread = 0, totalread = 0;
					do
					{
						nread = stream.Read (bytes, 0, len);
						Text = Text + System.Text.Encoding.UTF8.GetString (bytes, 0, len).Replace ("\n", "").Replace ("\r", "").Replace ("\t", "");
						if (Text.Replace (" ", "").ToLower ( ).Contains ("</title>"))
						{
							MatchCollection mc = regEx.Matches (Text);

							if (mc.Count > 0)
							{
								title = mc[0].Value.Replace ("<title>", "").Replace ("</title>", "");
							}
						}
						totalread += nread;
					} while (nread != 0 && string.IsNullOrEmpty (title));

					DateTime dt2 = DateTime.Now;

					double secs = ((double)(dt2 - dt1).Milliseconds) / 1000d;
					if (nread > 0)
					{
						intspeed += (int)Math.Round ((double)totalread / secs, 0);
						k++;
					}

					stream.Close ( );
				}
				catch (System.Net.WebException ex)
				{
					if (++i > 2)
					{
						vt.PrintText (//"\n Thread: " + Thread.CurrentThread.Name +
						"\n\n <Red,>Download aborted:</> " + ex.Message +
						" <" + url + ">\n");
						//if (abortafter == _abortafter.yes)
						//{
						//	abortafter = _abortafter.all;
						return null;
						//}
						//break;
					}
					vt.PrintText (//"\n Thread: " + Thread.CurrentThread.Name +
					"\n\n <Yellow,>Exception:</> " + ex.Message + " <" +
					url + "> " + i.ToString ( ) + " of 3 attempts.\n");
					Thread.Sleep (1500);
					continue;
				}
				if (i > 0 && i < 3)
				{
					vt.PrintText ("\n <Green,>Ok:</> " + (i + 1).ToString ( ) + " of 3 attempts.\n");
				}
				break;
			}
			if (string.IsNullOrEmpty (title))
			{
				title = "<" + url + ">";
			}

			return title;
		}

		public void Print (int k, float factor, int secs)
		{
			Print (k, docInfoList.Count, ref LastiDoc, ref LastTimeDate, factor, secs);
		}

		public void Print (int k, int len, ref int lastk, ref DateTime lasttd, float factor, int secs)
		{
			lock (printcritlock)
			{
				int per = k == -1 ? 100 : (k == -2 ? 0 : ((int)Math.Ceiling ((((float)k / (float)(len)) * 100f) * factor)));

				string sym = "";
				switch (k % 4)
				{
					case 0: sym = "|"; break;
					case 1: sym = "/"; break;
					case 2: sym = "─"; break;
					case 3: sym = "\\"; break;
				}

				DateTime end = DateTime.Now;

				if (lasttd == null)
				{
					lasttd = end;
				}

				TimeSpan elapsedtime = end - lasttd;

				int KBmean = (int)Math.Round (((double)totalbytessec / (double)readsteps) / 1024d, 0);
				KBmean = KBmean >= 0 ? KBmean : 0;

				string temp;

				if (k == lastk && elapsedtime.Duration ( ).Seconds > secs)
				{
					temp = per.ToString ( ) + "% " + sym + (KBmean > 0 ? (" [" + KBmean.ToString ( ) + " KBytes/s]") : "") +
					" ..." + elapsedtime.Duration ( ).ToString (@"h\:mm\:ss");
				}
				else
				{
					if (k != lastk)
					{
						lasttd = DateTime.Now;
					}

					lastk = k;
					temp = per.ToString ( ) + "% " + sym + (KBmean > 0 ? (" [" + KBmean.ToString ( ) + " KBytes/s]") : "");
				}
				// \x1b[1M
				vt.PrintText ("\r <BrightWhite,>" + temp + (temp.Length < 80 ? new string (' ', 80 - temp.Length) : "\n"));
			}
		}

		#endregion Methods of Workers (4)
	}
}