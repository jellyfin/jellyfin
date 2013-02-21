/*****************************************************************
|
|   Neptune - Network :: Winsock Implementation
|
|   (c) 2001-2006 Gilles Boccon-Gibod
|   Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   static initializer
+---------------------------------------------------------------------*/
class NPT_WinsockSystem {
public:
    static NPT_WinsockSystem Initializer;
    ~NPT_WinsockSystem();
    
private:
    NPT_WinsockSystem();
};
