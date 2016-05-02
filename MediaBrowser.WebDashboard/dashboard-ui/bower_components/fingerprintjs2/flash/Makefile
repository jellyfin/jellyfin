all: FontList.swf

FontList.swf: clean 
    mxmlc -static-link-runtime-shared-libraries FontList.as

clean:
    rm -f FontList.swf
