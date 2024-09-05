// Copyright (c) 2021-2024 Carpe Diem Software Developing by Alex Versetty.
// http://carpediem.0fees.us/
// License: MIT

using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace CDSD.Data
{
    class Serialization
	{
		public static string XmlSerialize<T>(T value)
		{
			if (value == null) return null;

			XmlSerializer serializer = new XmlSerializer(typeof(T));
			XmlWriterSettings settings = new XmlWriterSettings();

			settings.Encoding = new UnicodeEncoding(false, false);
			settings.Indent = true;
			settings.IndentChars = "    ";
			settings.OmitXmlDeclaration = false;

			using (StringWriter textWriter = new StringWriter())
			{
				using (XmlWriter xmlWriter = XmlWriter.Create(textWriter, settings))
                    serializer.Serialize(xmlWriter, value);
				
				return textWriter.ToString();
			}
		}

		public static T XmlDeserialize<T>(string xml)
		{
			if (string.IsNullOrEmpty(xml))
				return default;

			XmlSerializer serializer = new XmlSerializer(typeof(T));
			XmlReaderSettings settings = new XmlReaderSettings();

			using (StringReader textReader = new StringReader(xml))
				using (XmlReader xmlReader = XmlReader.Create(textReader, settings))
					return (T)serializer.Deserialize(xmlReader);
		}
	}
}
