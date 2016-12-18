//============================================================================
// BDInfo - Blu-ray Video and Audio Analysis Tool
// Copyright © 2010 Cinema Squid
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//=============================================================================


namespace BDInfo
{
    public abstract class TSCodecVC1
    {
        public static void Scan(
            TSVideoStream stream,
            TSStreamBuffer buffer,
            ref string tag)
        {
            int parse = 0;
            byte frameHeaderParse = 0;
            byte sequenceHeaderParse = 0;
            bool isInterlaced = false;

            for (int i = 0; i < buffer.Length; i++)
            {
                parse = (parse << 8) + buffer.ReadByte();

                if (parse == 0x0000010D)
                {
                    frameHeaderParse = 4;
                }
                else if (frameHeaderParse > 0)
                {
                    --frameHeaderParse;
                    if (frameHeaderParse == 0)
                    {
                        uint pictureType = 0;
                        if (isInterlaced)
                        {
                            if ((parse & 0x80000000) == 0)
                            {
                                pictureType = 
                                    (uint)((parse & 0x78000000) >> 13);
                            }
                            else
                            {
                                pictureType = 
                                    (uint)((parse & 0x3c000000) >> 12);
                            }
                        }
                        else
                        {
                            pictureType = 
                                (uint)((parse & 0xf0000000) >> 14);
                        }

                        if ((pictureType & 0x20000) == 0)
                        {
                            tag = "P";
                        }
                        else if ((pictureType & 0x10000) == 0)
                        {
                            tag = "B";
                        }
                        else if ((pictureType & 0x8000) == 0)
                        {
                            tag = "I";
                        }
                        else if ((pictureType & 0x4000) == 0)
                        {
                            tag = "BI";
                        }
                        else
                        {
                            tag = null;
                        }
                        if (stream.IsInitialized) return;
                    }
                }
                else if (parse == 0x0000010F)
                {
                    sequenceHeaderParse = 6;
                }
                else if (sequenceHeaderParse > 0)
                {
                    --sequenceHeaderParse;
                    switch (sequenceHeaderParse)
                    {
                        case 5:
                            int profileLevel = ((parse & 0x38) >> 3);
                            if (((parse & 0xC0) >> 6) == 3)
                            {
                                stream.EncodingProfile = string.Format(
                                    "Advanced Profile {0}", profileLevel);
                            }
                            else
                            {
                                stream.EncodingProfile = string.Format(
                                    "Main Profile {0}", profileLevel);
                            }
                            break;

                        case 0:
                            if (((parse & 0x40) >> 6) > 0)
                            {
                                isInterlaced = true;
                            }
                            else
                            {
                                isInterlaced = false;
                            }
                            break;
                    }
                    stream.IsVBR = true;
                    stream.IsInitialized = true;
                }
            }
        }
    }
}
