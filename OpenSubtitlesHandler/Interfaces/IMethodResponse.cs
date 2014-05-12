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
using System.ComponentModel;

namespace OpenSubtitlesHandler
{
    /// <summary>
    /// When you call a method to communicate with OpenSubtitles server, that method should return this response with the reuired information.
    /// </summary>
    public abstract class IMethodResponse
    {
        public IMethodResponse() { LoadAttributes(); }
        public IMethodResponse(string name, string message)
        {
            this.name = name;
            this.message = message;
        }
        protected string name;
        protected string message;
        protected double seconds;
        protected string status;

        protected virtual void LoadAttributes()
        {
            foreach (Attribute attr in Attribute.GetCustomAttributes(this.GetType()))
            {
                if (attr.GetType() == typeof(MethodResponseDescription))
                {
                    this.name = ((MethodResponseDescription)attr).Name;
                    this.message = ((MethodResponseDescription)attr).Message;
                    break;
                }
            }
        }

        [Description("The name of this response"), Category("MethodResponse")]
        public virtual string Name { get { return name; } set { name = value; } }
        [Description("The message about this response"), Category("MethodResponse")]
        public virtual string Message { get { return message; } set { message = value; } }
        [Description("Time taken to execute this command on server"), Category("MethodResponse")]
        public double Seconds { get { return seconds; } set { seconds = value; } }
        [Description("The status"), Category("MethodResponse")]
        public string Status { get { return status; } set { status = value; } }
    }
}
