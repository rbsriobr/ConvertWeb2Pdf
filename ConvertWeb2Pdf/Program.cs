using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Annotations;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace ConvertWeb2Pdf
{
	internal class Program
	{
		private static void Main (string[] args)
		{
			VirtTerm vt = new VirtTerm ( );

			Workers work = new Workers ( );

			work.options = "";

			work.iDoc = 1;

			work.DefForeColor = Console.ForegroundColor;

			string prodVer  = FileVersionInfo.GetVersionInfo (Assembly.GetExecutingAssembly ( ).Location).ProductVersion;
			string subject  = "";
			string author   = "";
			string keywords = "";

			// obtained from downloading samples with internet speed at 15Mbps
			float [] fac1 = new float[] { 0.0f, 0.015f, 0.922f, 0.934f, 0.945f, 0.962f, 0.999f, 1.0f };

			DateTime last = DateTime.Now;

			if (args.Length < 2)
			{
				vt.PrintText ("\n  ConvertWeb2Pdf version " + prodVer);
				vt.PrintText ("\n\n  Usage: ConvertWeb2Pdf <Yellow,>[options]</> <file.txt> <out.pdf>");
				vt.PrintText ("\n\n  Options:");
				vt.PrintText ("\n  <Yellow,>-nc</>, Do not compress pdf");
				vt.PrintText ("\n  <Yellow,>-dj</>, Disable javascripts");
				vt.PrintText ("\n  <Yellow,>-ni</>, Do not load images");
				vt.PrintText ("\n  <Yellow,>-lq</>, Draft quality");
				vt.PrintText ("\n  <Yellow,>-gs</>, Grayscale printing");
				vt.PrintText ("\n  <Yellow,>-ab</>, Abort after failure ");
				vt.PrintText ("\n  <Yellow,>-ti:</>, Timeout in ms (default is infinity) ");
				vt.PrintText ("\n  <Yellow,>-su:</>, Metadata: Pdf subject. Use <Yellow,>^</> as a space placeholder (e.g. <Yellow,>-su:Pdf^test</> becomes \"Pdf test\")");
				vt.PrintText ("\n  <Yellow,>-ar:</>, Metadata: Pdf author. Use <Yellow,>^</> as a space placeholder. Use <Yellow,>?</> as a Computer/User placeholder");
				vt.PrintText ("\n  <Yellow,>-kw:</>, Metadata: Pdf keywords. Comma-delimited items. Use <Yellow,>^</> as a space placeholder");
				vt.PrintText ("\n\n  This software is published under BSD (3-Clause) License.");
				vt.PrintText ("\n\n  PDFsharp and MigraDoc are published by empira Software GmbH under MIT License.");
				vt.PrintText ("\n  <BrightWhite,><http://www.pdfsharp.net/>");
				vt.PrintText ("\n\n  Wkhtmltopdf is published under LGPLv3 License.");
				vt.PrintText ("\n  <BrightWhite,><https://wkhtmltopdf.org/>");
				vt.PrintText ("\n\n  If you experience bugs or want to request new features please visit");
				vt.PrintText ("\n  <BrightWhite,><https://github.com/rbsriobr//ConvertWeb2Pdf/issues>");
				vt.PrintText ("\n\n  Copyright 2017 Ricardo Santos\n");
				return;
			}

			work.Print (-2, fac1[0], 2);

			for (int k = 0; k < args.Length - 2; k++)
			{
				if (args[k].ToLower ( ) == "-nc")
				{
					work.options += " --no-pdf-compression ";
				}
				else if (args[k].ToLower ( ) == "-dj")
				{
					work.options += " --disable-javascript ";
				}
				else if (args[k].ToLower ( ) == "-ni")
				{
					work.options += " --no-images ";
				}
				else if (args[k].ToLower ( ) == "-lq")
				{
					work.options += " --lowquality ";
				}
				else if (args[k].ToLower ( ) == "-gs")
				{
					work.options += " --grayscale ";
				}
				else if (args[k].ToLower ( ) == "-ab")
				{
					work.abortafter = Workers._abortafter.yes;
				}
				else if (args[k].ToLower ( ).Substring (0, 4) == "-ti:")
				{
					int res;
					if (Int32.TryParse (args[k].ToLower ( ).Substring (4), out res))
					{
						if (res < 0)
						{
							res = -1;
						}
						work.timeout = res;
					}
				}
				else if (args[k].ToLower ( ).Substring (0, 4) == "-su:")
				{
					subject = args[k].ToLower ( ).Substring (4);
					subject = subject.Replace ("^", " ");
				}
				else if (args[k].ToLower ( ).Substring (0, 4) == "-kw:")
				{
					keywords = args[k].ToLower ( ).Substring (4);
					keywords = keywords.Replace ("^", " ");
				}
				else if (args[k].ToLower ( ) == "-ar:?")
				{
					author = Environment.MachineName + "/" + Environment.UserName;
					author = author.Replace ("^", " ");
				}
				else if (args[k].ToLower ( ).Substring (0, 4) == "-ar:")
				{
					author = args[k].ToLower ( ).Substring (4);
					author = author.Replace ("^", " ");
				}
			}

			string InFilename  = args[args.Length - 2];
			string OutFilename = args[args.Length - 1];

			OutFilename = OutFilename.Replace (" ", "_").Replace (".pdf", "");

			if (!File.Exists (InFilename))
			{
				vt.PrintText ("\n\n <Red,>File not found:</> " + InFilename);
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
						vt.PrintText ("\r <Yellow,>Duplicate URL found in file.</> Lines: " + j.ToString ( ) + ", " + (h + 1).ToString ( ));
						j++;
						continue;
					}
					if (!Uri.TryCreate (Line, UriKind.Absolute, out test))
					{
						vt.PrintText ("\r <Yellow,>Invalid URL.</> Line: " + j.ToString ( ));
						j++;
						continue;
					}

					j++;

					Urilist.Add (Line);

					work.docInfoList.Add (new DocInfo (-1, -1, OutFilename + work.docInfoList.Count.ToString ( ) + ".pdf", Line, ""));
				}

				Urilist.Clear ( );

				inFile.Close ( );

				work.elemArray = new Object[(work.docInfoList.Count * 2) + 2];

				string now = DateTime.Now.ToString ( );
				string docname = "ConvertWeb2Pdf - " + now;

				now = now.Replace ("/", "").Replace ("\\", "").Replace (":", "").Replace (" ", "-");

				Directory.CreateDirectory (now);

				work.elemArray[0] = new XElement ("p", new XElement ("br"), docname);
				work.elemArray[1] = new XElement ("hr");

				work.exeFileName = Path.GetDirectoryName (System.Reflection.Assembly.GetEntryAssembly ( ).Location);
				work.exeFileName = work.exeFileName + "\\" + "wkhtmltopdf.exe";

				work.Print (work.iDoc, fac1[1], 2);

				work.fac = fac1[2];

				Directory.SetCurrentDirectory (Directory.GetCurrentDirectory ( ) + "\\" + now + "\\");

				work.curDir = Directory.GetCurrentDirectory ( );

				Console.CursorVisible = false;

				////////////////////
				//	Download web  //
				////////////////////

				Thread thread1 = new Thread (new ThreadStart (work.DoWork));

				thread1.Start ( );

				Thread thread2 = new Thread (new ThreadStart (work.DoWork));

				thread2.Start ( );

				Thread thread3 = new Thread (new ThreadStart (work.DoWork));

				thread3.Start ( );

				Thread thread4 = new Thread (new ThreadStart (work.DoWork));

				thread4.Start ( );

				bool stop = false;

				while (!stop)
				{
					stop = thread1.Join (250);
					stop &= thread2.Join (250);
					stop &= thread3.Join (250);
					stop &= thread4.Join (250);

					work.Print (work.iDoc, fac1[2], 2);
				}

				if ((work.abortafter == Workers._abortafter.yes) || (work.abortafter == Workers._abortafter.all))
				{
					return;
				}

				//////////////////////////////////////////////////////
				//	Test of corrupted pdf files and get page count  //
				//////////////////////////////////////////////////////

				int pagOffset = 1;

				for (int idoc = 0; idoc < work.docInfoList.Count; idoc++)
				{
					j = 0;

					if (work.docInfoList.ElementAt (idoc).Exists)
					{
						while (true)
						{
							try
							{
								using (PdfDocument inputDocument = PdfReader.Open (work.docInfoList.ElementAt (idoc).DocFileName, PdfDocumentOpenMode.Import))
								{
									work.docInfoList.ElementAt (idoc).NPags = inputDocument.PageCount;
									work.docInfoList.ElementAt (idoc).PagOffset = pagOffset;
									pagOffset += inputDocument.PageCount;
								}
							}
							catch (Exception ex)
							{
								if (++j > 2)
								{
									vt.PrintText ("\n\n <Red,>File deleteted: file seems corrupted.");
									try
									{
										File.Delete (work.docInfoList.ElementAt (idoc).DocFileName);
									}
									catch (System.IO.IOException)
									{ }
									break;
								}
								vt.PrintText ("\n\n <Yellow,>Exception on opening file</> <" + work.docInfoList.ElementAt (idoc).DocFileName +
									">: " + ex.Message + " " + j.ToString ( ) + " of 3 attempts.");
								Thread.Sleep (1500);
								continue;
							}
							break;
						}
						if (j > 0 && j < 3)
						{
							vt.PrintText ("\n\n <Green,>Ok:</> " + (j + 1).ToString ( ) + " of 3 attempts.");
							j = 0;
						}
					}
				}

				////////////////////////////////////////
				//	Create a html file with contents  //
				////////////////////////////////////////

				for (int idoc = 1; idoc < work.docInfoList.Count; idoc++)
				{
					if (work.docInfoList.ElementAt (idoc).Exists)
					{
						work.elemArray[idoc * 2] = new XElement ("p",
														new XElement ("a",
															new XAttribute ("href", work.docInfoList.ElementAt (idoc).Url),
															idoc.ToString ( ) + " - " + work.docInfoList.ElementAt (idoc).DocTitle));
					}
					else
					{
						work.elemArray[idoc * 2] = new XElement ("p",
																	new XElement ("font",
																		new XAttribute ("color", "red"),
																		idoc.ToString ( ) + " Failed to download !"));
					}
					work.elemArray[(idoc * 2) + 1] = new XElement ("p",
																	new XAttribute ("style", "text-indent: 2em"),
																	new XElement ("small", "\t\t" + work.docInfoList.ElementAt (idoc).Url));
				}

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

				work.totalbytessec = 0;

				work.Print (work.iDoc, fac1[3], 2);

				///////////////////////////
				//	Convert html to pdf  //
				///////////////////////////

				System.Diagnostics.Process pProcess = new System.Diagnostics.Process ( );

				//pProcess.ErrorDataReceived += pProcess_ErrorDataReceived;

				pProcess.StartInfo.CreateNoWindow   = true;
				pProcess.StartInfo.UseShellExecute  = false;
				pProcess.StartInfo.FileName         = work.exeFileName;
				pProcess.StartInfo.WorkingDirectory = work.curDir;

				pProcess.StartInfo.Arguments = work.options + " \"" + OutFilename + "0.html" + "\" \"" + OutFilename + "0.pdf";

				pProcess.Start ( );

				if (work.timeout >= 0)
				{
					bool state = pProcess.WaitForExit (work.timeout);
					if (!pProcess.HasExited)
					{
						pProcess.Kill ( );
					}
					if (!state)
					{
						try
						{
							File.Delete (OutFilename + "0.pdf");
						}
						catch (System.IO.IOException)
						{ }
						vt.PrintText ("\n\n <Yellow,>Process killed:</> the file <" + OutFilename + "0.pdf> was not created from <" +
							OutFilename + "0.html> because it exeeded timeout.");
					}
				}
				else
				{
					pProcess.WaitForExit ( );
				}

				work.Print (work.iDoc, fac1[4], 2);

				//////////////////
				//	Merge pdfs  //
				//////////////////

				int doc0len = 0;

				using (PdfDocument outputDocument = new PdfDocument ( ))
				{
					for (int idoc = 0; idoc < work.docInfoList.Count; idoc++)
					{
						if (work.docInfoList.ElementAt (idoc).Exists)
						{
							using (PdfDocument inputDocument = PdfReader.Open (OutFilename + idoc.ToString ( ) + ".pdf", PdfDocumentOpenMode.Import))
							{
								if (idoc == 0)
								{
									doc0len = inputDocument.PageCount;
								}
								for (int ipag = 0; ipag < inputDocument.PageCount; ipag++)
								{
									AddPage (outputDocument, inputDocument.Pages[ipag]);
								}
							}
						}
					}
					outputDocument.Save ("temp.pdf");
					outputDocument.Close ( );
				}

				////////////////////////
				//	Update pdf links  //
				////////////////////////

				Dictionary<string, int> Url2PagOffset = work.docInfoList.ToDictionary (p => p.Url).ToDictionary (i => i.Key, i => i.Value.PagOffset);

				work.Print (work.iDoc, fac1[5], 2);

				using (PdfDocument pdfDoc = PdfReader.Open ("temp.pdf", PdfDocumentOpenMode.Modify))
				{
					UpdateDoc (pdfDoc, Url2PagOffset, "Contents", doc0len);

					if (pdfDoc.Pages.Count > 0)
					{
						pdfDoc.Info.Creator = "ConvertWeb2Pdf vs " + prodVer;
						pdfDoc.Info.Subject = subject;
						pdfDoc.Info.Keywords = keywords;
						pdfDoc.Info.Author = author;

						pdfDoc.Save (OutFilename + ".pdf");
						pdfDoc.Close ( );
					}
				}

				work.Print (work.iDoc, fac1[6], 2);

				//////////////////////////////
				//	Delete temporary files  //
				//////////////////////////////

				try
				{
					for (int idoc = 0; idoc < work.docInfoList.Count; idoc++)
					{
						File.Delete (OutFilename + idoc.ToString ( ) + ".pdf");
					}

					File.Delete ("temp.pdf");
					File.Delete (OutFilename + "0.html");
				}
				catch (System.IO.IOException)
				{ }
			}
			catch (PdfSharp.Pdf.IO.PdfReaderException ex)
			{
				vt.PrintText ("\n\n <Red,>PdfSharp.Pdf.IO.PdfReaderException:</> " + ex.Message);
			}
			catch (Exception ex)
			{
				vt.PrintText ("\n\n <Red,>Exception:</> " + ex.Message);
			}

			work.Print (work.iDoc, fac1[7], 2);

			Console.CursorVisible = true;
		}

		private static void AddPage (PdfDocument destDoc, PdfPage inPage)
		{
			destDoc.AddPage (inPage);

			PdfPage outpage = destDoc.Pages[destDoc.PageCount - 1];

			foreach (PdfAnnotation anot in inPage.Annotations.OfType<PdfAnnotation> ( ))
			{
				PdfString uri = null;
				foreach (PdfDictionary dicitem in anot.Elements.Values.OfType<PdfDictionary> ( ))
				{
					uri = dicitem.Elements["/URI"] as PdfString;
				}
				if (uri != null)
				{
					outpage.AddWebLink (anot.Rectangle, uri.Value);
				}
			}
		}

		//static void pProcess_ErrorDataReceived (object sender, System.Diagnostics.DataReceivedEventArgs e)
		//{
		//	throw new NotImplementedException ( );
		//}

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

		private static bool URICompare (string str1, string str2)
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

		static public void UpdateDoc (PdfDocument doc, Dictionary<string, int> url2pagoffset, string pagtitle, int pag0len)
		{
			PdfString uri;

			for (int ipag = 0; ipag < doc.PageCount; ipag++)
			{
				PdfPage outpage = doc.Pages[ipag];

				for (int ianot = 0; ianot < outpage.Annotations.Count; ianot++)
				{
					PdfAnnotation anot = outpage.Annotations[ianot] as PdfAnnotation;

					foreach (PdfDictionary dicitem in anot.Elements.Values.OfType<PdfDictionary> ( ))
					{
						if ((uri = dicitem.Elements["/URI"] as PdfString) != null)
						{
							foreach (string urikey in url2pagoffset.Keys)
							{
								Uri test = null;
								string urikey2;
								try
								{
									urikey2 = RemoveEscEtc (urikey);

									if (Uri.TryCreate (urikey2, UriKind.Absolute, out test))
									{
										if (URICompare (uri.Value, urikey2))
										{
											PdfRectangle rect = anot.Rectangle as PdfRectangle;
											PdfRectangle newrect = rect.Clone ( );

											int pagnumb = url2pagoffset[urikey] + pag0len;
											PdfLinkAnnotation linkanot = outpage.AddDocumentLink (newrect, pagnumb);

											linkanot.Title = pagtitle;
											linkanot.Title += " (page " + pagnumb.ToString ( ) + ")";

											anot.Rectangle = new PdfRectangle (new XRect (0d, 0d, 0d, 0d));
										}
									}
								}
								catch (Exception)
								{ }
							}
						}
					}
				}
			}
		}
	}
}