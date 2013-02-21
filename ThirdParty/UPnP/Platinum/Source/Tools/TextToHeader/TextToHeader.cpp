/*****************************************************************
|
|   Platinum - Tool text to .h
|
| Copyright (c) 2004-2008, Plutinosoft, LLC.
| All rights reserved.
| http://www.plutinosoft.com
|
| This program is free software; you can redistribute it and/or
| modify it under the terms of the GNU General Public License
| as published by the Free Software Foundation; either version 2
| of the License, or (at your option) any later version.
|
| OEMs, ISVs, VARs and other distributors that combine and 
| distribute commercially licensed software with Platinum software
| and do not wish to distribute the source code for the commercially
| licensed software under version 2, or (at your option) any later
| version, of the GNU General Public License (the "GPL") must enter
| into a commercial license agreement with Plutinosoft, LLC.
| 
| This program is distributed in the hope that it will be useful,
| but WITHOUT ANY WARRANTY; without even the implied warranty of
| MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
| GNU General Public License for more details.
|
| You should have received a copy of the GNU General Public License
| along with this program; see the file LICENSE.txt. If not, write to
| the Free Software Foundation, Inc., 
| 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
| http://www.gnu.org/licenses/gpl-2.0.html
|
****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <sys/stat.h>

/*----------------------------------------------------------------------
|   globals
+---------------------------------------------------------------------*/
static struct {
    const char* in_filename;
    const char* variable_name;
    const char* header_name;
    const char* out_filename;
} Options;

/*----------------------------------------------------------------------
|   PrintUsageAndExit
+---------------------------------------------------------------------*/
static void
PrintUsageAndExit(char** args)
{
    fprintf(stderr, "usage: %s [-v <variable> -h <header name>] <intput> <output>\n", args[0]);
    fprintf(stderr, "-v : optional variable name\n");
    fprintf(stderr, "-h : optional header name\n");
    fprintf(stderr, "<input>  : input scpd filename\n");
    fprintf(stderr, "<output> : output filename\n");
    exit(1);
}

/*----------------------------------------------------------------------
|   ParseCommandLine
+---------------------------------------------------------------------*/
static void
ParseCommandLine(char** args)
{
    const char* arg;
    char** tmp = args+1;

    /* default values */
    Options.in_filename   = NULL;
    Options.variable_name = NULL;
    Options.out_filename  = NULL;

    while ((arg = *tmp++)) {
        if (!strcmp(arg, "-v")) {
            Options.variable_name = *tmp++;
        } else if (!strcmp(arg, "-h")) {
            Options.header_name = *tmp++;
        } else if (Options.in_filename == NULL) {
            Options.in_filename = arg;
        } else if (Options.out_filename == NULL) {
            Options.out_filename = arg;
        } else {
            fprintf(stderr, "ERROR: too many arguments\n");
            PrintUsageAndExit(args);
        }
    }

    /* check args */
    if (Options.in_filename == NULL) {
        fprintf(stderr, "ERROR: input filename missing\n");
        PrintUsageAndExit(args);
    }
    if (Options.out_filename == NULL) {
        fprintf(stderr, "ERROR: output filename missing\n");
        PrintUsageAndExit(args);
    }
}

/*----------------------------------------------------------------------
|   PrintHex
+---------------------------------------------------------------------*/
/*static void
PrintHex(unsigned char* h, unsigned int size)
{
    unsigned int i;
    for (i=0; i<size; i++) {
        printf("%c%c", 
               h[i]>>4 >= 10 ? 
               'A' + (h[i]>>4)-10 : 
               '0' + (h[i]>>4),
               (h[i]&0xF) >= 10 ? 
               'A' + (h[i]&0xF)-10 : 
               '0' + (h[i]&0xF));
    }
}*/

/*----------------------------------------------------------------------
|   PrintHexForHeader
+---------------------------------------------------------------------*/
static void
PrintHexForHeader(FILE* out, unsigned char h)
{
    fprintf(out, "0x%c%c", 
           h>>4 >= 10 ? 
           'A' + (h>>4)-10 : 
           '0' + (h>>4),
           (h&0xF) >= 10 ? 
           'A' + (h&0xF)-10 : 
           '0' + (h&0xF));
}

/*----------------------------------------------------------------------
|   main
+---------------------------------------------------------------------*/
int
main(int /*argc*/, char** argv)
{
    FILE*           in;
    FILE*           out;
    unsigned char*  data_block = NULL;
    unsigned long   data_block_size;
    unsigned long   k;
    unsigned char   col;
    
    /* parse command line */
    ParseCommandLine(argv);

    /* open input */
    in = fopen(Options.in_filename, "rb");
    if (in == NULL) {
        fprintf(stderr, "ERROR: cannot open input file (%s): %s\n", 
                Options.in_filename, strerror(errno));
    }

    /* read data in one chunk */
    {
        struct stat info;
        if (stat(Options.in_filename, &info)) {
            fprintf(stderr, "ERROR: cannot get input file size\n");
            return 1;
        }

        data_block_size = info.st_size;
        data_block = (unsigned char*)new unsigned char[data_block_size+1];
        if (data_block == NULL) {
            fprintf(stderr, "ERROR: out of memory\n");
            return 1;
        }

        if (fread(data_block, data_block_size, 1, in) != 1) {
            fprintf(stderr, "ERROR: cannot read input file\n");
            return 1;
        }
        data_block[data_block_size++] = 0;
    }

    /* open output */
    out = fopen(Options.out_filename, "w+");
    if (out == NULL) {
        fprintf(stderr, "ERROR: cannot open out output file (%s): %s\n", 
            Options.out_filename, strerror(errno));
    }
    fprintf(out,
"/*****************************************************************\n"
"|\n"
"|   Platinum - %s SCPD\n"
"|\n"
"| Copyright (c) 2004-2008, Plutinosoft, LLC.\n"
"| All rights reserved.\n"
"| http://www.plutinosoft.com\n"
"|\n"
"| This program is free software; you can redistribute it and/or\n"
"| modify it under the terms of the GNU General Public License\n"
"| as published by the Free Software Foundation; either version 2\n"
"| of the License, or (at your option) any later version.\n"
"|\n"
"| OEMs, ISVs, VARs and other distributors that combine and \n"
"| distribute commercially licensed software with Platinum software\n"
"| and do not wish to distribute the source code for the commercially\n"
"| licensed software under version 2, or (at your option) any later\n"
"| version, of the GNU General Public License (the \"GPL\") must enter\n"
"| into a commercial license agreement with Plutinosoft, LLC.\n"
"| \n"
"| This program is distributed in the hope that it will be useful,\n"
"| but WITHOUT ANY WARRANTY; without even the implied warranty of\n"
"| MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the\n"
"| GNU General Public License for more details.\n"
"|\n"
"| You should have received a copy of the GNU General Public License\n"
"| along with this program; see the file LICENSE.txt. If not, write to\n"
"| the Free Software Foundation, Inc., \n"
"| 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.\n"
"| http://www.gnu.org/licenses/gpl-2.0.html\n"
"|\n"
"****************************************************************/\n", 
		Options.header_name?Options.header_name:"");
		fprintf(out, "\n"
"/*----------------------------------------------------------------------\n"
"|   includes\n"
"+---------------------------------------------------------------------*/\n");
    fprintf(out, "#include \"NptTypes.h\"\n");
    fprintf(out, "\n"
"/*----------------------------------------------------------------------\n"
"|   globals\n"
"+---------------------------------------------------------------------*/\n");
    fprintf(out, "NPT_UInt8 %s[] =\n", 
    	  Options.variable_name?Options.variable_name:"kData");
    fprintf(out, "{\n  ");
    col = 0;
    
    /* rewind the input file */
    fseek(in, 0, SEEK_SET);

    for (k = 0; k < data_block_size; k++) {
        //PrintHex(&data_block[k], 1);
        PrintHexForHeader(out, data_block[k]);
        if (k < data_block_size - 1) fprintf(out, ", ");

        /* wrap around 20 columns */
        if (++col > 19) {
            col = 0;
            fprintf(out, "\n  ");
        }
    }

    /* print footer */
    fprintf(out, "\n};\n\n");  
    
    /* close file */
    fclose(out);

    /* close file */
    fclose(in);

    if (data_block) {
        delete[] data_block;
    }
    return 0;
}
