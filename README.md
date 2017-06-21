# ConvertWeb2Pdf

**This software downloads websites listed in a .txt file and converts them to a single .pdf file. (Website to pdf converter)**

ConvertWeb2Pdf version 1.1.0.0

  **Usage:** ConvertWeb2Pdf [options] <file.txt> <out.pdf>

Options:

  -nc, Do not compress pdf
  
  -dj, Disable javascripts
  
  -ni, Do not load images
  
  -lq, Draft quality
  
  -gs, Grayscale printing
  
  -ab, Abort after failure
  
  -ti:, Timeout in ms (default is infinity)
  
  -su:, Metadata: Pdf subject. Use ^ as a space placeholder (e.g. -su:Pdf^test becomes "Pdf test")
  
  -ar:, Metadata: Pdf author. Use ^ as a space placeholder. Use ? as a Computer/User placeholder
  
  -kw:, Metadata: Pdf keywords. Comma-delimited items. Use ^ as a space placeholder

  * This software is published under BSD (3-Clause) License.

  * [PDFsharp and MigraDoc](http://www.pdfsharp.net/ "http://www.pdfsharp.net/") are published by empira Software GmbH under MIT License.
  
  
  * [Wkhtmltopdf](https://wkhtmltopdf.org/ "https://wkhtmltopdf.org/") is published under LGPLv3 License.
  
  If you experience bugs or want to request new features please visit
  <https://github.com/rbsriobr//ConvertWeb2Pdf/issues>

  Copyright 2017 Ricardo Santos
