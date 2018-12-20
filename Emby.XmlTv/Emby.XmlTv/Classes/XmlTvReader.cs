using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Emby.XmlTv.Entities;

namespace Emby.XmlTv.Classes
{
    // Reads an XmlTv file
    public class XmlTvReader
    {
        private readonly string _fileName;
        private readonly string _language;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlTvReader" /> class.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="language">The specific language to return.</param>
        public XmlTvReader(string fileName, string language = null)
        {
            _fileName = fileName;

            // Normalize null/string.empty
            if (string.IsNullOrWhiteSpace(language))
            {
                language = null;
            }
            _language = language;
        }

        private XmlReader CreateXmlTextReader(string path)
        {
            XmlReaderSettings settings = new XmlReaderSettings();

            // https://msdn.microsoft.com/en-us/library/system.xml.xmlreadersettings.xmlresolver(v=vs.110).aspx
            // Looks like we don't need this anyway?
            // Starting with the .NET Framework 4.5.2, this setting has a default value of null.
            //settings.XmlResolver = null;

            settings.DtdProcessing = DtdProcessing.Ignore;

            settings.CheckCharacters = false;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreComments = true;
            //settings.ValidationType = ValidationType.None;

            return XmlReader.Create(path, settings);
        }

        /// <summary>
        /// Gets the list of channels present in the XML
        /// </summary>
        /// <returns></returns>
        public IEnumerable<XmlTvChannel> GetChannels()
        {
            var list = new List<XmlTvChannel>();

            using (var reader = CreateXmlTextReader(_fileName))
            {
                if (reader.ReadToDescendant("tv"))
                {
                    if (reader.ReadToDescendant("channel"))
                    {
                        do
                        {
                            var channel = GetChannel(reader);
                            if (channel != null)
                            {
                                list.Add(channel);
                            }
                        }
                        while (reader.ReadToFollowing("channel"));
                    }
                }
            }

            return list;
        }

        private XmlTvChannel GetChannel(XmlReader reader)
        {
            var id = reader.GetAttribute("id");

            if (string.IsNullOrEmpty(id))
            {
                // LogError("No id found for channel row");
                // Log.Error("  channel#{0} doesnt contain an id", iChannel);
                return null;
            }

            var result = new XmlTvChannel() { Id = id };

            using (var xmlChannel = reader.ReadSubtree())
            {
                xmlChannel.MoveToContent();
                xmlChannel.Read();

                // Read out the data for each node and process individually
                while (!xmlChannel.EOF && xmlChannel.ReadState == ReadState.Interactive)
                {
                    if (xmlChannel.NodeType == XmlNodeType.Element)
                    {
                        switch (xmlChannel.Name)
                        {
                            case "display-name":
                                ProcessNode(xmlChannel, s => result.DisplayName = s, _language, s => SetChannelNumber(result, s));
                                break;
                            case "url":
                                result.Url = xmlChannel.ReadElementContentAsString();
                                break;
                            case "icon":
                                result.Icon = ProcessIconNode(xmlChannel);
                                xmlChannel.Skip();
                                break;
                            default:
                                xmlChannel.Skip(); // unknown, skip entire node
                                break;
                        }
                    }
                    else
                    {
                        xmlChannel.Read();
                    }
                }
            }

            if (string.IsNullOrEmpty(result.DisplayName))
            {
                // LogError("No display-name found for channel {0}", id);
                return null;
            }

            return result;
        }

        private void SetChannelNumber(XmlTvChannel channel, string value)
        {
            value = value.Replace("-", ".");
            double number;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
            {
                channel.Number = value;
            }
        }

        /// <summary>
        /// Gets the programmes for a specified channel
        /// </summary>
        /// <param name="channelId">The channel id.</param>
        /// <param name="startDateUtc">The UTC start date.</param>
        /// <param name="endDateUtc">The UTC end date.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns></returns>
        public IEnumerable<XmlTvProgram> GetProgrammes(
                    string channelId,
                    DateTimeOffset startDateUtc,
                    DateTimeOffset endDateUtc,
                    CancellationToken cancellationToken)
        {
            var list = new List<XmlTvProgram>();

            using (var reader = CreateXmlTextReader(_fileName))
            {
                if (reader.ReadToDescendant("tv"))
                {
                    if (reader.ReadToDescendant("programme"))
                    {
                        do
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                continue; // Break out
                            }

                            var programme = GetProgramme(reader, channelId, startDateUtc, endDateUtc);
                            if (programme != null)
                            {
                                list.Add(programme);
                            }
                        }
                        while (reader.ReadToFollowing("programme"));
                    }
                }
            }

            return list;
        }

        public XmlTvProgram GetProgramme(XmlReader reader, string channelId, DateTimeOffset startDateUtc, DateTimeOffset endDateUtc)
        {
            var result = new XmlTvProgram();

            PopulateHeader(reader, result);

            using (var xmlProg = reader.ReadSubtree())
            {
                // First up, validate that this is the correct channel, and programme is within the time we are expecting
                if (!string.Equals(result.ChannelId, channelId, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                if (result.EndDate < startDateUtc || result.StartDate >= endDateUtc)
                {
                    return null;
                }

                xmlProg.MoveToContent();
                xmlProg.Read();

                // Loop through each element
                while (!xmlProg.EOF && xmlProg.ReadState == ReadState.Interactive)
                {
                    if (xmlProg.NodeType == XmlNodeType.Element)
                    {
                        switch (xmlProg.Name)
                        {
                            case "title":
                                ProcessTitleNode(xmlProg, result);
                                break;
                            case "category":
                                ProcessCategory(xmlProg, result);
                                break;
                            case "country":
                                ProcessCountry(xmlProg, result);
                                break;
                            case "desc":
                                ProcessDescription(xmlProg, result);
                                break;
                            case "sub-title":
                                ProcessSubTitle(xmlProg, result);
                                break;
                            case "new":
                                ProcessNew(xmlProg, result);
                                break;
                            case "previously-shown":
                                ProcessPreviouslyShown(xmlProg, result);
                                break;
                            case "quality":
                                ProcessQuality(xmlProg, result);
                                break;
                            case "episode-num":
                                ProcessEpisodeNum(xmlProg, result);
                                break;
                            case "date": // Copyright date
                                ProcessCopyrightDate(xmlProg, result);
                                break;
                            case "star-rating": // Community Rating
                                ProcessStarRating(xmlProg, result);
                                break;
                            case "rating": // Certification Rating
                                ProcessRating(xmlProg, result);
                                break;
                            case "credits":
                                if (xmlProg.IsEmptyElement)
                                {
                                    xmlProg.Skip();
                                }
                                else
                                {
                                    using (var subtree = xmlProg.ReadSubtree())
                                    {
                                        ProcessCredits(subtree, result);
                                    }
                                }
                                break;
                            case "icon":
                                result.Icon = ProcessIconNode(xmlProg);
                                xmlProg.Skip();
                                break;
                            case "premiere":
                                result.Premiere = new XmlTvPremiere();
                                // This was causing data after the premiere node to not be read. Reactivate this and debug if the premiere details are ever needed.
                                //ProcessPremiereNode(xmlProg, result);
                                xmlProg.Skip();
                                break;
                            default:
                                // unknown, skip entire node
                                xmlProg.Skip();
                                break;
                        }
                    }
                    else
                    {
                        xmlProg.Read();
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the list of supported languages in the XML
        /// </summary>
        /// <returns></returns>
        public List<XmlTvLanguage> GetLanguages(CancellationToken cancellationToken)
        {
            var results = new Dictionary<string, int>();

            //Loop through and parse out all elements and then lang= attributes
            //logger.LogInformation("Loading file {0}", _fileName);
            using (var reader = CreateXmlTextReader(_fileName))
            {
                while (reader.Read())
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        continue; // Break out
                    }

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        var language = reader.GetAttribute("lang");
                        if (!String.IsNullOrEmpty(language))
                        {
                            if (!results.ContainsKey(language))
                            {
                                results[language] = 0;
                            }
                            results[language]++;
                        }
                    }
                }
            }

            return
                results.Keys.Select(k => new XmlTvLanguage() { Name = k, Relevance = results[k] })
                    .OrderByDescending(l => l.Relevance)
                    .ToList();
        }

        private void ProcessCopyrightDate(XmlReader xmlProg, XmlTvProgram result)
        {
            var startValue = xmlProg.ReadElementContentAsString();
            if (string.IsNullOrEmpty(startValue))
            {
                // Log.Error("  programme#{0} doesnt contain a start date", iChannel);
                result.CopyrightDate = null;
            }
            else
            {
                var copyrightDate = ParseDate(startValue);
                if (copyrightDate != null)
                {
                    result.CopyrightDate = copyrightDate;
                }
            }
        }

        public void ProcessCredits(XmlReader creditsXml, XmlTvProgram result)
        {
            creditsXml.MoveToContent();
            creditsXml.Read();

            // Loop through each element
            while (!creditsXml.EOF && creditsXml.ReadState == ReadState.Interactive)
            {
                if (creditsXml.NodeType == XmlNodeType.Element)
                {
                    XmlTvCredit credit = null;
                    switch (creditsXml.Name)
                    {
                        case "director":
                            credit = new XmlTvCredit() { Type = XmlTvCreditType.Director };
                            break;
                        case "actor":
                            credit = new XmlTvCredit() { Type = XmlTvCreditType.Actor };
                            break;
                        case "writer":
                            credit = new XmlTvCredit() { Type = XmlTvCreditType.Writer };
                            break;
                        case "adapter":
                            credit = new XmlTvCredit() { Type = XmlTvCreditType.Adapter };
                            break;
                        case "producer":
                            credit = new XmlTvCredit() { Type = XmlTvCreditType.Producer };
                            break;
                        case "composer":
                            credit = new XmlTvCredit() { Type = XmlTvCreditType.Composer };
                            break;
                        case "editor":
                            credit = new XmlTvCredit() { Type = XmlTvCreditType.Editor };
                            break;
                        case "presenter":
                            credit = new XmlTvCredit() { Type = XmlTvCreditType.Presenter };
                            break;
                        case "commentator":
                            credit = new XmlTvCredit() { Type = XmlTvCreditType.Commentator };
                            break;
                        case "guest":
                            credit = new XmlTvCredit() { Type = XmlTvCreditType.Guest };
                            break;
                    }

                    if (credit != null)
                    {
                        credit.Name = creditsXml.ReadElementContentAsString();
                        result.Credits.Add(credit);
                    }
                    else
                    {
                        creditsXml.Skip();
                    }
                }
                else
                    creditsXml.Read();
            }
        }

        public void ProcessStarRating(XmlReader reader, XmlTvProgram result)
        {
            /*
             <star-rating>
              <value>3/3</value>
            </star-rating>
            */

            reader.ReadToDescendant("value");
            if (reader.Name == "value")
            {
                var textValue = reader.ReadElementContentAsString();
                if (textValue.Contains("/"))
                {
                    var components = textValue.Split('/');
                    float value;
                    if (float.TryParse(components[0], out value))
                    {
                        result.StarRating = value;
                    }
                }
            }
            else
            {
                reader.Skip();
            }
        }

        public void ProcessRating(XmlReader reader, XmlTvProgram result)
        {
            /*
            <rating system="MPAA">
                <value>TV-G</value>
            </rating>
            */

            var system = reader.GetAttribute("system");

            reader.ReadToDescendant("value");
            if (reader.Name == "value")
            {
                result.Rating = new XmlTvRating()
                {
                    System = system,
                    Value = reader.ReadElementContentAsString()
                };
            }
            else
            {
                reader.Skip();
            }
        }

        public void ProcessEpisodeNum(XmlReader reader, XmlTvProgram result)
        {
            /*
            <episode-num system="dd_progid">EP00003026.0666</episode-num>
            <episode-num system="onscreen">2706</episode-num>
            <episode-num system="xmltv_ns">.26/0.</episode-num>
            */

            var episodeSystem = reader.GetAttribute("system");
            switch (episodeSystem)
            {
                case "dd_progid":
                    ParseEpisodeDataForProgramId(reader, result);
                    break;
                case "icetv":
                    result.ProviderIds["icetv"] = reader.ReadElementContentAsString();
                    break;
                case "xmltv_ns":
                    ParseEpisodeDataForXmlTvNs(reader, result);
                    break;
                case "onscreen":
                    ParseEpisodeDataForOnScreen(reader, result);
                    break;
                case "thetvdb.com":
                    ParseTvdbSystem(reader, result);
                    break;
                case "imdb.com":
                    ParseImdbSystem(reader, result);
                    break;
                case "themoviedb.org":
                    ParseMovieDbSystem(reader, result);
                    break;
                case "SxxExx":
                    // TODO
                    // <episode-num system="SxxExx">S03E12</episode-num>
                    reader.Skip();
                    break;
                default: // Handles empty string and nulls
                    reader.Skip();
                    break;
            }
        }

        public void ParseMovieDbSystem(XmlReader reader, XmlTvProgram result)
        {
            // <episode-num system="thetvdb.com">series/248841</episode-num>
            // <episode-num system="thetvdb.com">episode/4749206</episode-num>

            var value = reader.ReadElementContentAsString();
            var parts = value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (string.Equals(parts[0], "series", StringComparison.OrdinalIgnoreCase))
            {
                result.SeriesProviderIds["tmdb"] = parts[1];
            }

            else if (parts.Length == 1 || string.Equals(parts[0], "episode", StringComparison.OrdinalIgnoreCase))
            {
                result.ProviderIds["tmdb"] = parts.Last();
            }
        }

        public void ParseImdbSystem(XmlReader reader, XmlTvProgram result)
        {
            // <episode-num system="imdb.com">series/tt1837576</episode-num>
            // <episode-num system="imdb.com">episode/tt3288596</episode-num>

            var value = reader.ReadElementContentAsString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }
            var parts = value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                return;
            }

            if (string.Equals(parts[0], "series", StringComparison.OrdinalIgnoreCase))
            {
                result.SeriesProviderIds["imdb"] = parts[1];
            }

            else if (string.Equals(parts[0], "episode", StringComparison.OrdinalIgnoreCase))
            {
                result.ProviderIds["imdb"] = parts[1];
            }
        }

        public void ParseTvdbSystem(XmlReader reader, XmlTvProgram result)
        {
            // <episode-num system="thetvdb.com">series/248841</episode-num>
            // <episode-num system="thetvdb.com">episode/4749206</episode-num>

            var value = reader.ReadElementContentAsString();
            var parts = value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                return;
            }

            if (string.Equals(parts[0], "series", StringComparison.OrdinalIgnoreCase))
            {
                result.SeriesProviderIds["tvdb"] = parts[1];
            }

            else if (string.Equals(parts[0], "episode", StringComparison.OrdinalIgnoreCase))
            {
                result.ProviderIds["tvdb"] = parts[1];
            }
        }

        public void ParseEpisodeDataForOnScreen(XmlReader reader, XmlTvProgram result)
        {
            //// example: 'Episode #FFEE' 
            //serEpNum = ConvertHTMLToAnsi(nodeEpisodeNum);
            //int num1 = serEpNum.IndexOf("#", 0);
            //if (num1 < 0) num1 = 0;
            //episodeNum = CorrectEpisodeNum(serEpNum.Substring(num1, serEpNum.Length - num1), 0);

            var value = reader.ReadElementContentAsString();
            // value = HttpUtility.HtmlDecode(value);
            value = value.Replace(" ", "");

            var hashIndex = value.IndexOf("#", StringComparison.Ordinal);
            if (hashIndex > -1)
            {
                // Take everything from the hash to the end.
                //TODO: This could be textual - how do we populate an Int32
                // result.EpisodeNumber
            }
        }

        public void ParseEpisodeDataForProgramId(XmlReader reader, XmlTvProgram result)
        {
            var value = reader.ReadElementContentAsString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                result.ProgramId = value;
            }
        }

        public void ParseEpisodeDataForXmlTvNs(XmlReader reader, XmlTvProgram result)
        {
            var value = reader.ReadElementContentAsString();

            value = value.Replace(" ", "");

            // Episode details
            var components = value.Split(new[] { "." }, StringSplitOptions.None);

            int parsedInt;

            if (!string.IsNullOrEmpty(components[0]))
            {
                // Handle either "5/12" or "5"
                var seriesComponents = components[0].Split(new[] { "/" }, StringSplitOptions.None);

                // handle the zero basing!
                if (int.TryParse(seriesComponents[0], out parsedInt))
                {
                    result.Episode.Series = parsedInt + 1;
                    if (seriesComponents.Length == 2)
                    {
                        if (int.TryParse(seriesComponents[1], out parsedInt))
                        {
                            result.Episode.SeriesCount = parsedInt;
                        }
                    }
                }
            }

            if (components.Length >= 2)
            {
                if (!string.IsNullOrEmpty(components[1]))
                {
                    // Handle either "5/12" or "5"
                    var episodeComponents = components[1].Split(new[] { "/" }, StringSplitOptions.None);

                    // handle the zero basing!
                    if (int.TryParse(episodeComponents[0], out parsedInt))
                    {
                        result.Episode.Episode = parsedInt + 1;
                        if (episodeComponents.Count() == 2)
                        {
                            if (int.TryParse(episodeComponents[1], out parsedInt))
                            {
                                result.Episode.EpisodeCount = parsedInt;
                            }
                        }
                    }
                }
            }

            if (components.Length >= 3)
            {
                if (!string.IsNullOrEmpty(components[2]))
                {
                    // Handle either "5/12" or "5"
                    var partComponents = components[2].Split(new[] { "/" }, StringSplitOptions.None);

                    // handle the zero basing!
                    if (int.TryParse(partComponents[0], out parsedInt))
                    {
                        result.Episode.Part = parsedInt + 1;
                        if (partComponents.Count() == 2)
                        {
                            if (int.TryParse(partComponents[1], out parsedInt))
                            {
                                result.Episode.PartCount = parsedInt;
                            }
                        }
                    }
                }
            }
        }

        public void ProcessQuality(XmlReader reader, XmlTvProgram result)
        {
            result.Quality = reader.ReadElementContentAsString();
        }

        public void ProcessPreviouslyShown(XmlReader reader, XmlTvProgram result)
        {
            // <previously-shown start="20070708000000" />
            var value = reader.GetAttribute("start");
            if (!string.IsNullOrEmpty(value))
            {
                // TODO: this may not be correct = validate it
                result.PreviouslyShown = ParseDate(value);
                if (result.PreviouslyShown != result.StartDate)
                {
                    result.IsPreviouslyShown = true;
                }
            }
            else
            {
                result.IsPreviouslyShown = true;
            }

            reader.Skip(); // Move on
        }

        public void ProcessNew(XmlReader reader, XmlTvProgram result)
        {
            result.IsNew = true;
            reader.Skip(); // Move on
        }

        public void ProcessCategory(XmlReader reader, XmlTvProgram result)
        {
            /*
            <category lang="en">News</category>
            */

            result.Categories = result.Categories ?? new List<string>();
            ProcessMultipleNodes(reader, s => result.Categories.Add(s), _language);

            //result.Categories.Add(reader.ReadElementContentAsString());
        }
        public void ProcessCountry(XmlReader reader, XmlTvProgram result)
        {
            /*
            <country>Canadá</country>
            <country>EE.UU</country>
            */

            result.Countries = result.Countries ?? new List<string>();
            ProcessNode(reader, s => result.Countries.Add(s), _language);
        }

        public void ProcessSubTitle(XmlReader reader, XmlTvProgram result)
        {
            /*
            <sub-title lang="en">Gino&apos;s Italian Escape - Islands in the Sun: Southern Sardinia Celebrate the Sea</sub-title>
            <sub-title lang="en">8782</sub-title>
            */
            ProcessNode(reader, s => result.Episode.Title = s, _language);
        }

        public void ProcessDescription(XmlReader reader, XmlTvProgram result)
        {
            ProcessNode(reader, s => result.Description = s, _language);
        }

        public void ProcessTitleNode(XmlReader reader, XmlTvProgram result)
        {
            // <title lang="en">Gino&apos;s Italian Escape</title>
            ProcessNode(reader, s => result.Title = s, _language);
        }

        public void ProcessPremiereNode(XmlReader reader, XmlTvProgram result)
        {
            // <title lang="en">Gino&apos;s Italian Escape</title>
            ProcessNode(reader,
                s =>
                {
                    if (result.Premiere == null)
                    {
                        result.Premiere = new XmlTvPremiere() { Details = s };
                    }
                    else
                    {
                        result.Premiere.Details = s;
                    }
                }, _language);
        }

        public XmlTvIcon ProcessIconNode(XmlReader reader)
        {
            var result = new XmlTvIcon();
            var isPopulated = false;

            var source = reader.GetAttribute("src");
            if (!String.IsNullOrEmpty(source))
            {
                result.Source = source;
                isPopulated = true;
            }

            var widthString = reader.GetAttribute("width");
            var width = 0;
            if (!String.IsNullOrEmpty(widthString) && Int32.TryParse(widthString, out width))
            {
                result.Width = width;
                isPopulated = true;
            }

            var heightString = reader.GetAttribute("height");
            var height = 0;
            if (!String.IsNullOrEmpty(heightString) && Int32.TryParse(heightString, out height))
            {
                result.Height = height;
                isPopulated = true;
            }

            return isPopulated ? result : null;
        }


        //public void ProcessNodeWithLanguage(XmlReader reader, Action<string> setter)
        //{
        //    var currentElementName = reader.Name;
        //    var result = string.Empty;
        //    var resultCandidate = reader.ReadElementContentAsString();
        //    var exactMatchFound = false;

        //    while (reader.Name == currentElementName)
        //    {
        //        var language = reader.GetAttribute("lang");
        //        resultCandidate = reader.ReadElementContentAsString();

        //        if (language == _language && !exactMatchFound)
        //        {
        //            result = resultCandidate;
        //        }

        //        reader.Skip();
        //    }

        //    result = String.IsNullOrEmpty(result) ? resultCandidate : result;
        //    setter(result);
        //}

        public void ProcessNode(XmlReader reader, Action<string> setter, string languageRequired = null, Action<string> allOccurrencesSetter = null)
        {
            /*
            <title lang="es">Homes Under the Hammer - Spanish</title>
		    <title lang="es">Homes Under the Hammer - Spanish 2</title>
		    <title lang="en">Homes Under the Hammer - English</title>
		    <title lang="en">Homes Under the Hammer - English 2</title>
		    <title lang="">Homes Under the Hammer - Empty Language</title>
		    <title lang="">Homes Under the Hammer - Empty Language 2</title>
		    <title>Homes Under the Hammer - No Language</title>
		    <title>Homes Under the Hammer - No Language 2</title>
            */

            /*  Expected Behaviour:
                - Language = Null   Homes Under the Hammer - No Language
                - Language = ""   Homes Under the Hammer - No Language
                - Language = es     Homes Under the Hammer - Spanish
                - Language = en     Homes Under the Hammer - English
            */

            var results = new List<Tuple<string, string>>();

            // We will always use the first value - so that if there are no matches we can return something
            var currentElementName = reader.Name;

            var lang = reader.HasAttributes ? reader.GetAttribute("lang") : null;
            var currentValue = reader.ReadElementContentAsString();
            results.Add(new Tuple<string, string>(currentValue, lang));

            if (allOccurrencesSetter != null)
            {
                allOccurrencesSetter(currentValue);
            }

            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == currentElementName)
                    {
                        lang = reader.HasAttributes ? reader.GetAttribute("lang") : null;
                        currentValue = reader.ReadElementContentAsString();

                        if (allOccurrencesSetter != null)
                        {
                            allOccurrencesSetter(currentValue);
                        }

                        results.Add(new Tuple<string, string>(currentValue, lang));
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            if (languageRequired != null)
            {
                foreach (var result in results)
                {
                    if (string.Equals(languageRequired, result.Item2, StringComparison.OrdinalIgnoreCase))
                    {
                        setter(result.Item1);
                        return;
                    }
                }
            }

            foreach (var result in results)
            {
                if (string.IsNullOrWhiteSpace(result.Item2))
                {
                    setter(result.Item1);
                    return;
                }
            }

            foreach (var result in results)
            {
                setter(result.Item1);
                return;
            }
        }

        public void ProcessMultipleNodes(XmlReader reader, Action<string> setter, string languageRequired = null)
        {
            /*
            <category lang="en">Property - English</category>
		    <category lang="en">Property - English 2</category>
		    <category lang="es">Property - Spanish</category>
		    <category lang="es">Property - Spanish 2</category>
		    <category lang="">Property - Empty Language</category>
		    <category lang="">Property - Empty Language 2</category>
		    <category>Property - No Language</category>
		    <category>Property - No Language 2</category>
            */

            /*  Expected Behaviour:
                - Language = Null   Property - No Language / Property - No Language 2
                - Language = ""     Property - Empty Language / Property - Empty Language 2
                - Language = es     Property - Spanish / Property - Spanish 2
                - Language = en     Property - English / Property - English 2
            */

            var currentElementName = reader.Name;
            var values = new[] { new { Language = reader.GetAttribute("lang"), Value = reader.ReadElementContentAsString() } }.ToList();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name != currentElementName)
                {
                    break;
                }

                if (reader.NodeType == XmlNodeType.Element && reader.Name == currentElementName)
                {
                    values.Add(new { Language = reader.GetAttribute("lang"), Value = reader.ReadElementContentAsString() });
                }
            }

            if (values.Count(v => v.Language == languageRequired) > 0)
            {
                values.RemoveAll(v => v.Language != languageRequired);
            }

            // ENumerate and return all the matches
            foreach (var result in values)
            {
                setter(result.Value);
            }
        }

        public void ProcessMultipleNodesWithLanguage(XmlReader reader, Action<string> setter)
        {
            var currentElementName = reader.Name;
            while (reader.Name == currentElementName)
            {
                var language = reader.GetAttribute("lang");
                if (String.IsNullOrEmpty(_language) || String.IsNullOrEmpty(language) || language == _language)
                {
                    setter(reader.ReadElementContentAsString());
                }
                reader.Skip();
            }
        }

        private void PopulateHeader(XmlReader reader, XmlTvProgram result)
        {
            result.ChannelId = reader.GetAttribute("channel");

            var startValue = reader.GetAttribute("start");
            if (string.IsNullOrEmpty(startValue))
            {
                // Log.Error("  programme#{0} doesnt contain a start date", iChannel);
                result.StartDate = DateTimeOffset.MinValue;
            }
            else
            {
                result.StartDate = ParseDate(startValue).GetValueOrDefault();
            }


            var endValue = reader.GetAttribute("stop");
            if (string.IsNullOrEmpty(endValue))
            {
                // Log.Error("  programme#{0} doesnt contain an end date", iChannel);
                result.EndDate = DateTimeOffset.MinValue;
            }
            else
            {
                result.EndDate = ParseDate(endValue).GetValueOrDefault();
            }
        }

        public static Regex _regDateWithOffset = new Regex(@"^(?<dateDigits>[0-9]{4,14})(\s(?<dateOffset>[+-]*[0-9]{1,4}))?$");

        public DateTimeOffset? ParseDate(string dateValue)
        {
            /*
            All dates and times in this DTD follow the same format, loosely based
            on ISO 8601.  They can be 'YYYYMMDDhhmmss' or some initial
            substring, for example if you only know the year and month you can
            have 'YYYYMM'.  You can also append a timezone to the end; if no
            explicit timezone is given, UTC is assumed.  Examples:
            '200007281733 BST', '200209', '19880523083000 +0300'.  (BST == +0100.)
            */

            DateTimeOffset? result = null;

            if (!string.IsNullOrEmpty(dateValue))
            {
                var completeDate = "20000101000000";
                var dateComponent = string.Empty;
                var dateOffset = "+00:00";

                var match = _regDateWithOffset.Match(dateValue);
                if (match.Success)
                {
                    dateComponent = match.Groups["dateDigits"].Value;
                    if (!String.IsNullOrEmpty(match.Groups["dateOffset"].Value))
                    {
                        dateOffset = match.Groups["dateOffset"].Value; // Add in the colon to ease parsing later
                        if (dateOffset.Length == 5)
                        {
                            dateOffset = dateOffset.Insert(3, ":"); // Add in the colon to ease parsing later
                        }
                        else
                        {
                            dateOffset = "+00:00";
                        }
                    }
                }

                // Pad out the date component part to 14 characaters so 2016061509 becomes 20160615090000
                if (dateComponent.Length < 14)
                {
                    dateComponent = dateComponent + completeDate.Substring(dateComponent.Length, completeDate.Length - dateComponent.Length);
                }

                var standardDate = String.Format("{0} {1}", dateComponent, dateOffset);
                DateTimeOffset parsedDateTime;
                if (DateTimeOffset.TryParseExact(standardDate, "yyyyMMddHHmmss zzz", CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDateTime))
                {
                    return parsedDateTime.ToUniversalTime();
                }
                else
                {
                    //Logger.LogWarning("Unable to parse the date {0} from standardised form {1}", dateValue, standardDate);
                }
            }

            return result;
        }

        public string StandardiseDate(string value)
        {
            var completeDate = "20000101000000";
            var dateComponent = string.Empty;
            var dateOffset = "+0000";

            var match = _regDateWithOffset.Match(value);
            if (match.Success)
            {
                dateComponent = match.Groups["dateDigits"].Value;
                dateOffset = match.Groups["dateOffset"].Value;
            }

            // Pad out the date component part to 14 characaters so 2016061509 becomes 20160615090000
            if (dateComponent.Length < 14)
            {
                dateComponent = dateComponent + completeDate.Substring(dateComponent.Length, completeDate.Length - dateComponent.Length);
            }

            return String.Format("{0} {1}", dateComponent, dateOffset);
        }
    }
}
