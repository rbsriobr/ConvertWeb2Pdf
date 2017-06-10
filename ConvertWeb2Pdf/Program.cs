using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Annotations;
using PdfSharp.Pdf.Advanced;
using System.IO;
using System.Net;
using System.Threading;

namespace ConvertWeb2Pdf
{
	class Program
	{
		class DocInfo
		{
			public DocInfo (int npags, int pagoffset, string outfilename, string url, string pagtitle)
			{
				OutFileName = outfilename;
				Url = url;
				NPags = npags;
				PagOffset = pagoffset;
				DocTitle = pagtitle;
			}
			public int NPags { get; set; }
			public int PagOffset { get; set; }
			public string Url { get; set; }
			public string OutFileName { get; set; }
			public string DocTitle { get; set; }
		}

		//class ElemArrayComparer : IEqualityComparer<int[]>
		//{
		//	private int idx2comp = -1;

		//	public ElemArrayComparer (int _idx2comp)
		//	{
		//		idx2comp = _idx2comp;
		//	}

		//	public bool Equals (int[] x, int[] y)
		//	{
		//		return x[idx2comp] == y[idx2comp];
		//	}

		//	public int GetHashCode (int[] obj)
		//	{
		//		return string.Join (",", obj).GetHashCode ( );
		//	}
		//}

		static void Main (string[] args)
		{
			Work work = new Work ( );

			work.iDoc = 1;

			if (args.Length != 2)
			{
				Console.WriteLine ("\n  Usage: ConvertWeb2Pdf <file.txt> <out.pdf>");
				Console.WriteLine ("\n  This software is published under BSD (3-Clause) License.");
				Console.WriteLine ("  PDFsharp and MigraDoc are published by empira Software GmbH under MIT License.");
				Console.WriteLine ("  Wkhtmltopdf is published under LGPLv3 License.");
				Console.WriteLine ("\n  If you experience bugs or want to request new features please visit");
				Console.WriteLine ("  <https://github.com/rbsriobr//ConvertWeb2Pdf/issues>");
				Console.WriteLine ("\n  Copyright 2017 Ricardo Santos");
				return;
			}

			string InFilename  = args[0];
			string OutFilename = args[1];
			OutFilename = OutFilename.Replace (".pdf", "");


			if (!File.Exists (InFilename))
			{
				Console.WriteLine ("File not found: " + InFilename);
				return;
			}

			try
			{
				String Line;

				StreamReader inFile = new StreamReader (InFilename);

				work.docInfoList.Add (new DocInfo (-1, -1, OutFilename + "0.pdf", OutFilename, ""));

				List<string> Urilist = new List<string> ( );

				int j = 1, h;

				Uri test = null;

				while ((Line = inFile.ReadLine ( )) != null)
				{
					Line = RemoveEscEtc (Line);

					if (String.IsNullOrEmpty (Line))
					{
						j++;
						continue;
					}

					if ((h = Urilist.IndexOf (Line)) != -1)
					{
						Console.WriteLine ("\rDuplicate URL found in file. Lines: " + j.ToString ( ) + ", " + (h + 1).ToString ( ));
						return;
					}
					if (!Uri.TryCreate (Line, UriKind.Absolute, out test))
					{
						Console.WriteLine ("\rInvalid URL. Line: " + j.ToString ( ));
						return;
					}

					j++;

					Urilist.Add (Line);

					work.docInfoList.Add (new DocInfo (-1, -1, OutFilename + work.docInfoList.Count.ToString ( ) + ".pdf", Line, ""));
				}

				Urilist.Clear ( );

				inFile.Close ( );

				work.elemArray = new Object[work.docInfoList.Count + 2];

				string now = DateTime.Now.ToString ( );
				string docname = "ConvertWeb2Pdf - " + now;

				now = now.Replace ("/", "").Replace ("\\", "").Replace (":", "").Replace (" ", "-");

				Directory.CreateDirectory (now);

				work.elemArray[0] = new XElement ("p", new XElement ("br"), docname);
				work.elemArray[1] = new XElement ("hr");

				work.exeFileName = Path.GetDirectoryName (System.Reflection.Assembly.GetEntryAssembly ( ).Location);
				work.exeFileName = work.exeFileName + "\\" + "wkhtmltopdf.exe";

				Directory.SetCurrentDirectory (Directory.GetCurrentDirectory ( ) + "\\" + now + "\\");

				work.curDir = Directory.GetCurrentDirectory ( );

				Console.CursorVisible = false;

				Thread thread1 = new Thread (new ThreadStart (work.DoWork));

				thread1.Start ( );

				Thread thread2 = new Thread (new ThreadStart (work.DoWork));

				thread2.Start ( );

				Thread thread3 = new Thread (new ThreadStart (work.DoWork));

				thread3.Start ( );

				Thread thread4 = new Thread (new ThreadStart (work.DoWork));

				thread4.Start ( );

				thread1.Join ( );
				thread2.Join ( );
				thread3.Join ( );
				thread4.Join ( );

				var xDocument = new XDocument (
					new XDocumentType ("html", null, null, null),
						new XElement ("html",
						new XElement ("head",
							new XElement ("title", docname),
							new XElement ("meta",
								new XAttribute ("content", "text/html; charset=UTF-8"),
								new XAttribute ("http-equiv", "content-type"))),
						new XElement ("body", work.elemArray)
						));

				var settings = new XmlWriterSettings
				{
					OmitXmlDeclaration = true,
					Indent = true,
					IndentChars = "\t"
				};

				using (var writer = XmlWriter.Create (OutFilename + "0.html", settings))
				{
					xDocument.WriteTo (writer);
				}

				System.Diagnostics.Process pProcess = new System.Diagnostics.Process ( );

				pProcess.ErrorDataReceived += pProcess_ErrorDataReceived;

				pProcess.StartInfo.CreateNoWindow = true;
				pProcess.StartInfo.UseShellExecute = false;
				pProcess.StartInfo.FileName = work.exeFileName;
				pProcess.StartInfo.WorkingDirectory = work.curDir;

				pProcess.StartInfo.Arguments = "\"" + OutFilename + "0.html" + "\" \"" + OutFilename + "0.pdf";

				pProcess.Start ( );

				pProcess.WaitForExit ( );

				work.Print (-1);

				PdfDocument inputDocument = null;

				int pagOffset = 1;

				for (int i= 0; i < work.docInfoList.Count; i++)
				{
					inputDocument = PdfReader.Open (work.docInfoList.ElementAt (i).OutFileName, PdfDocumentOpenMode.Import);
					work.docInfoList.ElementAt (i).NPags = inputDocument.PageCount;
					work.docInfoList.ElementAt (i).PagOffset = pagOffset;
					pagOffset += inputDocument.PageCount;
					//inputDocument.Close ( );
				}

				PdfDocument outputDocument = new PdfDocument ( );

				Dictionary<string, DocInfo> Url2Info = work.docInfoList.ToDictionary (p => p.Url);

				Dictionary<string, int> Url2PagOffset = Url2Info.ToDictionary (i => i.Key, i => i.Value.PagOffset);

				// merge docs
				for (int idoc = 0; idoc < work.docInfoList.Count; idoc++)
				{
					inputDocument = PdfReader.Open (work.docInfoList.ElementAt (idoc).OutFileName, PdfDocumentOpenMode.Import);

					string DocTitle = work.docInfoList.ElementAt (idoc).DocTitle;

					for (int ipag = 0; ipag < inputDocument.PageCount; ipag++)
					{
						PdfPage page = inputDocument.Pages[ipag];
						outputDocument.AddPage (page);
					}
				}

				outputDocument.Save ("temp.pdf");

				outputDocument.Close ( );

				outputDocument = new PdfDocument ( );

				inputDocument = PdfReader.Open ("temp.pdf", PdfDocumentOpenMode.Import);

				for (int ipag = 0; ipag < inputDocument.PageCount; ipag++)
				{
					PdfPage page = inputDocument.Pages[ipag];

					page = UpdatePage (page, Url2PagOffset, "Contents");

					outputDocument.AddPage (page);
				}

				outputDocument.Save (OutFilename + ".pdf");

				outputDocument.Close ( );

				File.Delete ("temp.pdf");
				File.Delete (OutFilename + "0.html");

			}
			catch (Exception ex)
			{
				Console.WriteLine ("Exception: " + ex.Message);
			}

			Console.CursorVisible = true;
		}

		static void pProcess_ErrorDataReceived (object sender, System.Diagnostics.DataReceivedEventArgs e)
		{
			throw new NotImplementedException ( );
		}

		static public string RemoveEscEtc (string text, bool removefragment = true)
		{
			String [] esc = new[] { " ", "\a", "\b", "\t", "\r", "\v", "\f", "\n" };
			var replacements = esc.ToDictionary (item => item, value => "");
			string newtext = replacements.Aggregate (text, (current, replacement) => current.Replace (replacement.Key, replacement.Value));
			if (removefragment)
			{
				string [] seg = newtext.Split ('#');
				if (seg.Length > 1)
				{
					newtext = seg[0];
				}
			}
			return newtext;
		}


		static bool URICompare (string str1, string str2)
		{
			if (Uri.Compare (
				new Uri (str1), new Uri (str2),
							UriComponents.Host | UriComponents.PathAndQuery, //UriComponents.Scheme |
							UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0)
			{
				return true;
			}
			return false;
		}

		static public PdfPage UpdatePage (PdfPage page, Dictionary<string, int> url2pagoffset, string pagtitle)
		{
			PdfString uri;

			for (int ianot = 0; ianot < page.Annotations.Count; ianot++)
			{
				PdfAnnotation anot = page.Annotations.ElementAt (ianot) as PdfAnnotation;

				PdfRectangle rect = anot.Rectangle as PdfRectangle;

				PdfRectangle newrect = rect.Clone ( );

				foreach (PdfItem kpitem in anot.Elements.Values)
				{
					PdfDictionary dicitem = kpitem as PdfDictionary;

					if (dicitem != null)
					{
						if ((uri = dicitem.Elements["/URI"] as PdfString) != null)
						{
							foreach (string urikey in url2pagoffset.Keys)
							{
								string urikey2 = RemoveEscEtc (urikey);

								Uri test = null;
								if (Uri.TryCreate (urikey2, UriKind.Absolute, out test))
								{
									if (URICompare (uri.Value, urikey2))
									{
										page.Annotations.Remove (anot);
										PdfLinkAnnotation linkanot = page.AddDocumentLink (newrect, url2pagoffset[urikey2]);
										linkanot.Title = pagtitle;
										linkanot.Title += " (page " + url2pagoffset[urikey2].ToString ( ) + ")";
										ianot = -1; // iterate again from 0
										break;
									}
								}
							}
						}
					}
					if (ianot == -1)
					{
						break;
					}
				}
			}
			return page;
		}

		class Work
		{
			public int iDoc           = 1;
			public String exeFileName = "";
			public String curDir      = "";
			public Object [] elemArray;
			private Regex regEx = new Regex ("<title>(.*?)</title>");

			public List<DocInfo> docInfoList = new List<DocInfo> ( );

			public void Print (int curDoc)
			{
				int per = curDoc < 0 ? 100 : (int)(((float)curDoc / (float)docInfoList.Count) * 100f);

				string sym = "";
				switch (curDoc % 4)
				{
					case 0: sym = "| "; break;
					case 1: sym = "/ "; break;
					case 2: sym = "─ "; break;
					case 3: sym = "\\ "; break;
				}

				Console.Write ("\r " + per.ToString ( ) + "% " + sym + "       ");
			}

			private string GetTitle (ref WebClient wc, string url)
			{
				String title = "";

				int i = 0;

				//Uri test = null;
				//if (!Uri.TryCreate (url, UriKind.Absolute, out test))
				//{
				//	return "<" + url + ">";
				//}

				while (true)
				{
					try
					{
						string html = wc.DownloadString (url);

						html = html.Replace ("\n", "").Replace ("\r", "").Replace ("\t", "");

						MatchCollection mc = regEx.Matches (html);

						if (mc.Count > 0)
						{
							title = mc[0].Value.Replace ("<title>", "").Replace ("</title>", "");
						}
					}
					catch (System.Net.WebException ex)
					{
						if (++i < 3)
						{
							Console.WriteLine ("Exception: " + ex.Message + " <" + url + ">");
							break;
						}
						Console.Write (" " + i.ToString ( ) + "/3");
						Thread.Sleep (500);
						continue;
					}
					if (string.IsNullOrEmpty (title))
					{
						title = "<" + url + ">";
					}
					break;
				}
				return title;
			}

			public void DoWork ( )
			{
				WebClient wc = new WebClient ( );

				String Url  = null;
				int curDoc;

				while (true)
				{
					curDoc = iDoc++;

					if (curDoc >= docInfoList.Count)
					{
						iDoc = docInfoList.Count;
						break;
					}

					Print (curDoc);

					DocInfo docInfo = docInfoList.ElementAt (curDoc);

					Url = docInfo.Url;

					docInfo.DocTitle = GetTitle (ref wc, Url);

					System.Diagnostics.Process pProcess = new System.Diagnostics.Process ( );

					pProcess.ErrorDataReceived += pProcess_ErrorDataReceived;

					if (!File.Exists (exeFileName))
					{
						Console.WriteLine ("File not found: " + exeFileName);
						return;
					}

					pProcess.StartInfo.CreateNoWindow = true;
					pProcess.StartInfo.UseShellExecute = false;
					pProcess.StartInfo.FileName = exeFileName;
					pProcess.StartInfo.WorkingDirectory = curDir;

					pProcess.StartInfo.Arguments = "\"" + Url + "\" \"" + docInfo.OutFileName + "\"";

					elemArray[curDoc + 2] = new XElement ("p",
						new XElement ("a",
							new XAttribute ("href", Url),
							curDoc.ToString ( ) + " - " + docInfo.DocTitle));

					pProcess.Start ( );

					pProcess.WaitForExit ( );
				}
			}
		}
	}
}
