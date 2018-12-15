using System;
using System.Linq;

namespace ImageMagickSharp
{
    public enum VirtualPixelType : int
	{
        Undefined = 0,  
        Background = 1,  
        Constant = 2,  /* deprecated */   
        Dither = 3,   
        Edge = 4,   
        Mirror = 5,   
        Random = 6,  
        Tile = 7,   
        Transparent =8 ,   
        Mask = 9,   
        Black = 10,   
        Gray = 11,   
        White = 12,  
        HorizontalTile =13 ,   
        VerticalTile = 14,  
        HorizontalTileEdge = 15, 
        VerticalTileEdge = 16,  
        CheckerTile = 17
	}
}
