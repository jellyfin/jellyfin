/*****************************************************************
|
|   Neptune - Network :: Xbox Winsock Implementation
|
|   (c) 2001-2005 Gilles Boccon-Gibod
|   Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   static initializer
+---------------------------------------------------------------------*/
class NPT_WinsockSystem {
public:
    static NPT_WinsockSystem Initializer;
private:
    NPT_WinsockSystem();
    ~NPT_WinsockSystem();
};
