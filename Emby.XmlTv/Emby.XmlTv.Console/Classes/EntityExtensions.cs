using System;
using System.Linq;
using System.Text;

using Emby.XmlTv.Entities;

namespace Emby.XmlTv.Console.Classes
{
    public static class EntityExtensions
    {
        public static string GetHeader(this string text)
        {
            var channelHeaderString = " " + text;

            var builder = new StringBuilder();
            builder.AppendLine("".PadRight(5 + channelHeaderString.Length + 5, Char.Parse("*")));
            builder.AppendLine("".PadRight(5, Char.Parse("*")) + channelHeaderString + "".PadRight(5, Char.Parse("*")));
            builder.AppendLine("".PadRight(5 + channelHeaderString.Length + 5, Char.Parse("*")));

            return builder.ToString();
        }

        public static string GetChannelDetail(this XmlTvChannel channel)
        {
            var builder = new StringBuilder();
            builder.AppendFormat("Id:                {0}\r\n", channel.Id);
            builder.AppendFormat("Display-Name:      {0}\r\n", channel.DisplayName);
            builder.AppendFormat("Url:               {0}\r\n", channel.Url);
            builder.AppendFormat("Icon:              {0}\r\n", channel.Icon != null ? channel.Icon.ToString() : string.Empty);
            builder.AppendLine("-------------------------------------------------------");

            return builder.ToString();
        }

        public static string GetProgrammeDetail(this XmlTvProgram programme, XmlTvChannel channel)
        {
            var builder = new StringBuilder();
            builder.AppendFormat("Channel:           {0} - {1}\r\n", channel.Id, channel.DisplayName);
            builder.AppendFormat("Start Date:        {0:G}\r\n", programme.StartDate);
            builder.AppendFormat("End Date:          {0:G}\r\n", programme.EndDate);
            builder.AppendFormat("Name:              {0}\r\n", programme.Title);
            builder.AppendFormat("Episode Detail:    {0}\r\n", programme.Episode);
            builder.AppendFormat("Episode Title:     {0}\r\n", programme.Episode.Title);
            builder.AppendFormat("Description:       {0}\r\n", programme.Description);
            builder.AppendFormat("Categories:        {0}\r\n", string.Join(", ", programme.Categories));
            builder.AppendFormat("Countries:         {0}\r\n", string.Join(", ", programme.Countries));
            builder.AppendFormat("Credits:           {0}\r\n", string.Join(", ", programme.Credits));
            builder.AppendFormat("Rating:            {0}\r\n", programme.Rating);
            builder.AppendFormat("Star Rating:       {0}\r\n", programme.StarRating.HasValue ? programme.StarRating.Value.ToString() : string.Empty);
            builder.AppendFormat("Previously Shown:  {0:G}\r\n", programme.PreviouslyShown);
            builder.AppendFormat("Copyright Date:    {0:G}\r\n", programme.CopyrightDate);
            builder.AppendFormat("Is Repeat:         {0}\r\n", programme.IsPreviouslyShown);
            builder.AppendFormat("Icon:              {0}\r\n", programme.Icon != null ? programme.Icon.ToString() : string.Empty);
            builder.AppendLine("-------------------------------------------------------");
            return builder.ToString();
        }
    }
}
