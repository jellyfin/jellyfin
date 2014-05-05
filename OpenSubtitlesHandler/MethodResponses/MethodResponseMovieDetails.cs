/* This file is part of OpenSubtitles Handler
   A library that handle OpenSubtitles.org XML-RPC methods.

   Copyright © Ala Ibrahim Hadid 2013

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;

namespace OpenSubtitlesHandler
{
    [MethodResponseDescription("MovieDetails method response",
         "MovieDetails method response hold all expected values from server.")]
    public class MethodResponseMovieDetails : IMethodResponse
    {
        public MethodResponseMovieDetails()
            : base()
        { }
        public MethodResponseMovieDetails(string name, string message)
            : base(name, message)
        { }
        // Details
        private string id;
        private string title;
        private string year;
        private string coverLink;
       
        private string duration;
        private string tagline;
        private string plot;
        private string goofs;
        private string trivia;
        private List<string> cast = new List<string>();
        private List<string> directors = new List<string>();
        private List<string> writers = new List<string>();
        private List<string> awards = new List<string>();
        private List<string> genres = new List<string>();
        private List<string> country = new List<string>();
        private List<string> language = new List<string>();
        private List<string> certification = new List<string>();

        // Details
        public string ID { get { return id; } set { id = value; } }
        public string Title { get { return title; } set { title = value; } }
        public string Year { get { return year; } set { year = value; } }
        public string CoverLink { get { return coverLink; } set { coverLink = value; } }
        public string Duration { get { return duration; } set { duration = value; } }
        public string Tagline { get { return tagline; } set { tagline = value; } }
        public string Plot { get { return plot; } set { plot = value; } }
        public string Goofs { get { return goofs; } set { goofs = value; } }
        public string Trivia { get { return trivia; } set { trivia = value; } }
        public List<string> Cast { get { return cast; } set { cast = value; } }
        public List<string> Directors { get { return directors; } set { directors = value; } }
        public List<string> Writers { get { return writers; } set { writers = value; } }
        public List<string> Genres { get { return genres; } set { genres = value; } }
        public List<string> Awards { get { return awards; } set { awards = value; } }
        public List<string> Country { get { return country; } set { country = value; } }
        public List<string> Language { get { return language; } set { language = value; } }
        public List<string> Certification { get { return certification; } set { certification = value; } }
    }
}
