/*****************************************************************
|
|      XML Test Program 1
|
|      (c) 2001-2003 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include <stdlib.h>
#include <assert.h>
#include "Neptune.h"
#include "NptDebug.h"

/*----------------------------------------------------------------------
|       CHECK
+---------------------------------------------------------------------*/
#define CHECK(test)                                     \
do {                                                    \
    if (!(test)) {                                      \
        fprintf(stderr, "FAILED: line %d\n", __LINE__); \
        assert(0);                                      \
    }                                                   \
} while(0)

/*----------------------------------------------------------------------
|       TestWriter
+---------------------------------------------------------------------*/
static void
TestWriter()
{
    NPT_XmlElementNode* top = new NPT_XmlElementNode("top");
    NPT_XmlElementNode* child1 = new NPT_XmlElementNode("child1");
    child1->SetAttribute("someAttribute", "someValue");
    top->AddChild(child1);
    NPT_XmlElementNode* child2 = new NPT_XmlElementNode("child2");
    child2->SetAttribute("someOtherAttribute", "someOtherValue");
    child2->AddText("Some Text");
    child1->AddChild(child2);
    NPT_XmlElementNode* child3 = new NPT_XmlElementNode("child3");
    child3->SetAttribute("thirdArrtibute", "3");
    child2->AddChild(child3);
    
    NPT_XmlWriter writer;
    NPT_File out(NPT_FILE_STANDARD_OUTPUT);
    out.Open(NPT_FILE_OPEN_MODE_WRITE);
    NPT_OutputStreamReference out_stream;
    out.GetOutputStream(out_stream);
    
    writer.Serialize(*top, *out_stream);
}

#if defined(_WIN32) && defined(_DEBUG) && !defined(UNDER_CE)
#include <crtdbg.h>
#endif

/*----------------------------------------------------------------------
|       TestFinders
+---------------------------------------------------------------------*/
static void
TestFinders()
{
    const char* xml = "<a b='foo' c='bar' ns:b='bla' ns:d='123' xmlns:ns='ns-uri' xmlns:ns1='ns1-uri' xmlns:ns2='ns2-uri' xmlns:ns3='ns3-uri'><b xmlns='ns4-uri' ba='123' ns2:bo='345'></b><b ba='123' ns2:bo='345'></b><ns2:b></ns2:b><ns1:b></ns1:b></a>";
    NPT_XmlParser parser;
    NPT_XmlNode* root;
    CHECK(NPT_SUCCEEDED(parser.Parse(xml, root)));
    
    NPT_XmlElementNode* elem = root->AsElementNode();
    const NPT_String* attr = elem->GetAttribute("d");
    CHECK(attr == NULL);
    attr = elem->GetAttribute("b");
    CHECK(attr != NULL && *attr == "foo");
    attr = elem->GetAttribute("b", "ns-uri");
    CHECK(attr != NULL && *attr == "bla");
    attr = elem->GetAttribute("c", NPT_XML_ANY_NAMESPACE);
    CHECK(attr != NULL && *attr == "bar");
    attr = elem->GetAttribute("b", NPT_XML_ANY_NAMESPACE);
    CHECK(attr != NULL && *attr == "foo");
    attr = elem->GetAttribute("b", "boubou");
    CHECK(attr == NULL);
    attr = elem->GetAttribute("d", NPT_XML_NO_NAMESPACE);
    CHECK(attr == NULL);

    NPT_XmlElementNode* child;
    child = elem->GetChild("b");
    CHECK(child != NULL && *child->GetAttribute("ba") == "123");
    child = elem->GetChild("b", "ns4-uri");
    CHECK(child != NULL &&  child->GetAttribute("ba", "ns4-uri") == NULL);
    CHECK(child != NULL && *child->GetAttribute("bo", NPT_XML_ANY_NAMESPACE) == "345");
    CHECK(child != NULL &&  child->GetAttribute("bo", NPT_XML_NO_NAMESPACE)  == NULL);
    CHECK(child != NULL &&  child->GetAttribute("bo", "foo") == NULL);
    CHECK(child != NULL && *child->GetAttribute("bo", "ns2-uri") == "345");
    child = elem->GetChild("b", NPT_XML_ANY_NAMESPACE);
    CHECK(child != NULL && *child->GetNamespace() == "ns4-uri");
    child = elem->GetChild("b", "ns2-uri");
    CHECK(child != NULL && *child->GetNamespace() == "ns2-uri");
    child = elem->GetChild("b", "boubou");
    CHECK(child == NULL);

    delete root;
}

/*----------------------------------------------------------------------
|       TestNamespaces
+---------------------------------------------------------------------*/
static void
TestNamespaces()
{
    NPT_XmlElementNode* top = new NPT_XmlElementNode("top");
    top->SetNamespaceUri("", "http://namespace1.com");
    CHECK(top->GetNamespaceUri("") &&
        *(top->GetNamespaceUri("")) == "http://namespace1.com");

    NPT_XmlElementNode* child1 = new NPT_XmlElementNode("child1");
    top->AddChild(child1);
    CHECK(child1->GetNamespaceUri(""));
    CHECK(*(child1->GetNamespaceUri("")) == "http://namespace1.com");

    NPT_XmlElementNode* child2 = new NPT_XmlElementNode("ns1", "child2");
    top->AddChild(child2);
    CHECK(child2->GetNamespaceUri(""));
    CHECK(*(child2->GetNamespaceUri("")) == "http://namespace1.com");
    CHECK(child2->GetNamespaceUri("ns1") == NULL);
    child2->SetNamespaceUri("ns1", "http://blabla");
    CHECK(child2->GetNamespaceUri("ns1"));
    CHECK(*child2->GetNamespaceUri("ns1") == "http://blabla");
    CHECK(*child2->GetNamespace() == "http://blabla");

    // testing a child with a namespace defined in parent
    NPT_XmlElementNode* child3 = new NPT_XmlElementNode("ns1", "child3");
    child2->AddChild(child3);
    CHECK(child3->GetNamespaceUri(""));
    CHECK(*(child3->GetNamespaceUri("")) == "http://namespace1.com");
    CHECK(child3->GetNamespaceUri("ns1"));
    CHECK(*child3->GetNamespaceUri("ns1") == "http://blabla");
    CHECK(*child3->GetNamespace() == "http://blabla");

    // testing adding a namespace in a node which namespace is defined in parent
    child3->SetNamespaceUri("ns3", "http://foofoo");
    CHECK(child3->GetNamespaceUri("ns1"));
    CHECK(*child3->GetNamespaceUri("ns1") == "http://blabla");
    CHECK(*child3->GetNamespace() == "http://blabla");

    const char* xml1 = 
        "<top>"
        "  <child1 xmlns:foo='blabla'><cc1 foo:attr1='0'/></child1>"
        "  <child2 xmlns='foobar' attr1='1'>"
        "    <cc2/>"
        "    <cc3 />"
        "  </child2 >"
        "  <ns2:child3 xmlns:ns2='abcd'><cc3/></ns2:child3>"
        "  <child4 ns3:attr1='3' xmlns:ns3='efgh'>"
        "    <ns3:cc4 ns3:attr1='4'/>"
        "  </child4>"
        "</top>";
    NPT_XmlParser parser;
    NPT_XmlNode* root = NULL;
    NPT_Result result = parser.Parse(xml1, root);
    CHECK(NPT_SUCCEEDED(result));
    CHECK(root != NULL);

    NPT_XmlWriter    writer;
    NPT_MemoryStream output;
    writer.Serialize(*root, output);
    NPT_LargeSize size;
    output.GetSize(size);
    printf("%s", NPT_String((const char*)output.GetData(), (NPT_Size)size).GetChars());

    delete top;
    delete root;

    // test default and empty namespaces 
    const char* xml2 = "<top><a></a><b xmlns='foo'><c xmlns=''></c></b></top>";
    result = parser.Parse(xml2, root);
    CHECK(root->AsElementNode()->GetNamespace() == NULL);
    NPT_XmlElementNode* a_elem = (*root->AsElementNode()->GetChildren().GetItem(0))->AsElementNode();
    CHECK(a_elem->GetNamespace() == NULL);
    NPT_XmlElementNode* b_elem = (*root->AsElementNode()->GetChildren().GetItem(1))->AsElementNode();
    CHECK(*b_elem->GetNamespace() == "foo");
    NPT_XmlElementNode* c_elem = (*b_elem->GetChildren().GetItem(0))->AsElementNode();
    CHECK(c_elem->GetNamespace() == NULL);

    delete root;
}

/*----------------------------------------------------------------------
|       TestSerializer
+---------------------------------------------------------------------*/
static void
TestSerializer()
{
    NPT_XmlWriter    writer;
    NPT_MemoryStream output;
    NPT_String       check;
    NPT_LargeSize    size;

    //
    // test without namespaces
    //

    // simple element with no prefix and no namespace
    NPT_XmlElementNode* top = new NPT_XmlElementNode("top");
    writer.Serialize(*top, output);
    output.GetSize(size);
    check.Assign((const char*)output.GetData(), (NPT_Size)size);
    CHECK(check == "<top/>");

    // with one attribute
    output.SetDataSize(0);
    top->SetAttribute("attr1", "b&w");
    writer.Serialize(*top, output);
    output.GetSize(size);
    check.Assign((const char*)output.GetData(), (NPT_Size)size);
    CHECK(check == "<top attr1=\"b&amp;w\"/>");

    // add one child
    output.SetDataSize(0);
    delete top;
    top = new NPT_XmlElementNode("top");
    NPT_XmlElementNode* child1 = new NPT_XmlElementNode("child1");
    top->AddChild(child1);
    writer.Serialize(*top, output);
    output.GetSize(size);
    check.Assign((const char*)output.GetData(), (NPT_Size)size);
    CHECK(check == "<top><child1/></top>");

    //
    // test with namespaces
    //

    // test default namespaces
    output.SetDataSize(0);
    delete top;
    top = new NPT_XmlElementNode("top");
    top->SetNamespaceUri("", "http://namespace.com");
    writer.Serialize(*top, output);
    output.GetSize(size);
    check.Assign((const char*)output.GetData(), (NPT_Size)size);
    CHECK(check == "<top xmlns=\"http://namespace.com\"/>");

    // test attribute prefixes
    output.SetDataSize(0);
    delete top;
    top = new NPT_XmlElementNode("top");
    top->SetAttribute(NULL,  "foo", "6");
    top->SetAttribute("ns1", "foo", "3");
    top->SetAttribute("ns2", "foo", "4");
    top->SetAttribute("ns1", "foo", "5");
    writer.Serialize(*top, output);
    output.GetSize(size);
    check.Assign((const char*)output.GetData(), (NPT_Size)size);
    CHECK(check == "<top foo=\"6\" ns1:foo=\"5\" ns2:foo=\"4\"/>");

    delete top;
}

/*----------------------------------------------------------------------
|       TestCanonicalizer
+---------------------------------------------------------------------*/
static void
TestCanonicalizer()
{
    extern const char* xml_cano_1[];

    NPT_XmlParser parser(true); // do not ignore whitespaces
    NPT_XmlNode* root;

    for (unsigned int i=0; xml_cano_1[i]; i+=2) {
        const char* xml_in = xml_cano_1[i];
        const char* xml_out = xml_cano_1[i+1];
        CHECK(NPT_SUCCEEDED(parser.Parse(xml_in, root)));
        CHECK(root);

        NPT_XmlCanonicalizer canonicalizer;
        NPT_MemoryStream buffer1;
        NPT_Result result = canonicalizer.Serialize(*root, buffer1);

        NPT_String str((const char*)buffer1.GetData(), buffer1.GetDataSize());
        NPT_Debug("%s", str.GetChars());
        CHECK(str == xml_out);

        delete root;

        CHECK(NPT_SUCCEEDED(parser.Parse(str, root)));
        CHECK(root);
        NPT_MemoryStream buffer2;
        result = canonicalizer.Serialize(*root, buffer2);
        CHECK(buffer1.GetBuffer() == buffer2.GetBuffer());

        delete root;
    }
}

/*----------------------------------------------------------------------
|       TestRegression
+---------------------------------------------------------------------*/
static void
TestRegression()
{
    // test for a bug found when the XML parser would try
    // to compare a null prefix
    NPT_XmlElementNode* element = new NPT_XmlElementNode("hello");
    element->SetAttribute("ns", "foo", "6");
    element->SetAttribute("foo", "5");
    element->SetAttribute("ns", "foo", "7");
    element->SetAttribute("foo", "8");
    element->SetNamespaceUri("ns", "blabla");
    CHECK(*element->GetAttribute("foo") == "8");
    CHECK(*element->GetAttribute("foo", "blabla") == "7");
    
    delete element;
}

/*----------------------------------------------------------------------
|       TestWhitespace
+---------------------------------------------------------------------*/
static void
TestWhitespace()
{
    const char* xml = 
"<doc>\r\n"
"   <clean>   </clean>\r\n"
"   <dirty>   A   B   </dirty>\r\n"
"   <mixed>\r\n"
"      A\r\n"
"      <clean>   </clean>\r\n"
"      B\r\n"
"      <dirty>   A   B   </dirty>\r\n"
"      C\r\n"
"   </mixed>\r\n"
"</doc>\r\n";

    const char* expect1 = 
"<doc><clean/><dirty>   A   B   </dirty><mixed>\n"
"      A\n"
"      <clean/>\n"
"      B\n"
"      <dirty>   A   B   </dirty>\n"
"      C\n"
"   </mixed></doc>";

    const char* expect2 = 
"<doc>\n"
"   <clean>   </clean>\n"
"   <dirty>   A   B   </dirty>\n"
"   <mixed>\n"
"      A\n"
"      <clean>   </clean>\n"
"      B\n"
"      <dirty>   A   B   </dirty>\n"
"      C\n"
"   </mixed>\n"
"</doc>";

    NPT_XmlParser parser1(false); // ignore whitespace
    NPT_XmlNode* root;
    CHECK(NPT_SUCCEEDED(parser1.Parse(xml, root)));
    CHECK(root);

    NPT_XmlWriter writer;
    NPT_MemoryStream buffer;
    writer.Serialize(*root, buffer);
    CHECK(buffer.GetBuffer() == NPT_DataBuffer(expect1, NPT_StringLength(expect1)));

    delete root;

    NPT_XmlParser parser2(true); // keep whitespace
    CHECK(NPT_SUCCEEDED(parser2.Parse(xml, root)));
    CHECK(root);

    buffer.SetDataSize(0);
    writer.Serialize(*root, buffer);
    CHECK(buffer.GetBuffer() == NPT_DataBuffer(expect2, NPT_StringLength(expect2)));

    delete root;
}

/*----------------------------------------------------------------------
|       TestComments
+---------------------------------------------------------------------*/
static void
TestComments()
{
    const char* xml = 
        "<!-- comment outside the element -->\n"
        "<doc> blabla <!-- --> foo <!-- you <g> &foo -> &bar --> blibli</doc>";
    const char* expect = "<doc> blabla  foo  blibli</doc>";

    NPT_XmlParser parser;
    NPT_XmlNode* root;
    CHECK(NPT_SUCCEEDED(parser.Parse(xml, root)));
    CHECK(root);

    NPT_XmlWriter writer;
    NPT_MemoryStream buffer;
    writer.Serialize(*root, buffer);
    CHECK(buffer.GetBuffer() == NPT_DataBuffer(expect, NPT_StringLength(expect)));

    delete root;
}

/*----------------------------------------------------------------------
|       TestCdata
+---------------------------------------------------------------------*/
static void
TestCdata()
{
    const char* xml = 
        "<doc> blabla <![CDATA[  < [[  Smith ]] >   ]]> blibli</doc>";
    const char* expect = "<doc> blabla   &lt; [[  Smith ]] &gt;    blibli</doc>";

    NPT_XmlParser parser;
    NPT_XmlNode* root;
    CHECK(NPT_SUCCEEDED(parser.Parse(xml, root)));
    CHECK(root);

    NPT_XmlWriter writer;
    NPT_MemoryStream buffer;
    writer.Serialize(*root, buffer);
    CHECK(buffer.GetBuffer() == NPT_DataBuffer(expect, NPT_StringLength(expect)));

    delete root;
}

/*----------------------------------------------------------------------
|       TestAttributes
+---------------------------------------------------------------------*/
static void
TestAttributes()
{
    const char* xml = 
        "<element foo='blabla'><cc1 attr1='0'/></element>";
    NPT_XmlParser parser;
    NPT_XmlNode* root = NULL;
    NPT_Result result = parser.Parse(xml, root);
    CHECK(NPT_SUCCEEDED(result));
    CHECK(root != NULL);
    CHECK(root->AsElementNode() != NULL);
    const NPT_String* a = root->AsElementNode()->GetAttribute("foo");
    CHECK(*a == "blabla");
    delete root;
}

/*----------------------------------------------------------------------
|       TestAttributeNormalization
+---------------------------------------------------------------------*/
static void
TestAttributeNormalization()
{
    const char* xml = "<x a='\n\n xyz abc &#xD; &#xA; &#x9; &#x20; 12\r\n3\r4\n5 6  '/>";
    NPT_XmlParser parser;
    NPT_XmlNode* root = NULL;
    NPT_Result result = parser.Parse(xml, root);
    CHECK(NPT_SUCCEEDED(result));
    CHECK(root != NULL);
    CHECK(root->AsElementNode() != NULL);
    const NPT_String* a = root->AsElementNode()->GetAttribute("a");
    CHECK(*a == "   xyz abc \r \n \t   12 3 4 5 6  ");
    delete root;
}


/*----------------------------------------------------------------------
|       TestMakeStandalone
+---------------------------------------------------------------------*/
static void
TestMakeStandalone()
{
    const char* xml = 
        "<parent xmlns:foo='foo-ns' xmlns:bar='bar-ns' xmlns='default-ns'><inter xmlns:bli='bli-ns' xmlns:bou='bou-ns'><child><foo:a>a-text</foo:a><bar:b xml:fifi='0'>b-text</bar:b><c>c-text</c><d bou:att='b-att'/></child></inter></parent>";
    const char* expected = 
        "<child xmlns=\"default-ns\" xmlns:foo=\"foo-ns\" xmlns:bar=\"bar-ns\" xmlns:bou=\"bou-ns\"><foo:a>a-text</foo:a><bar:b xml:fifi=\"0\">b-text</bar:b><c>c-text</c><d bou:att=\"b-att\"/></child>";
    NPT_XmlParser parser;
    NPT_XmlNode* root = NULL;
    NPT_Result result = parser.Parse(xml, root);
    CHECK(NPT_SUCCEEDED(result));
    CHECK(root != NULL);
    CHECK(root->AsElementNode() != NULL);
    NPT_XmlElementNode* inter = root->AsElementNode()->GetChild("inter", NPT_XML_ANY_NAMESPACE);
    CHECK(inter != NULL);
    NPT_XmlElementNode* child = inter->GetChild("child", NPT_XML_ANY_NAMESPACE);
    CHECK(child != NULL);
    child->MakeStandalone();
    NPT_XmlWriter writer;
    NPT_MemoryStream buffer;
    writer.Serialize(*child, buffer);
    CHECK(buffer.GetBuffer() == NPT_DataBuffer(expected, NPT_StringLength(expected)));
    

    delete root;
}

/*----------------------------------------------------------------------
|       TestFile
+---------------------------------------------------------------------*/
static void
TestFile(const char* filename)
{
    NPT_InputStreamReference stream;
    NPT_Result               result;

    // open the input file
    NPT_File input(filename);
    result = input.Open(NPT_FILE_OPEN_MODE_READ);
    if (NPT_FAILED(result)) {
        NPT_Debug("XmtTest1:: cannot open input (%d)\n", result);
        return;
    }
    result = input.GetInputStream(stream);
    
    // parse the buffer
    NPT_XmlParser parser;
    NPT_XmlNode*  tree = NULL;
    result = parser.Parse(*stream, tree);
    if (NPT_FAILED(result)) {
        NPT_Debug("XmlTest1:: cannot parse input (%d)\n", result);
        return;
    }


    // dump the tree
    NPT_XmlWriter writer(2);
    NPT_File output(NPT_FILE_STANDARD_OUTPUT);
    output.Open(NPT_FILE_OPEN_MODE_WRITE);
    NPT_OutputStreamReference output_stream_ref;
    output.GetOutputStream(output_stream_ref);
    writer.Serialize(*tree, *output_stream_ref);

    // delete the tree
    delete tree;
}

/*----------------------------------------------------------------------
|       TestBadInput
+---------------------------------------------------------------------*/
static void
TestBadInput()
{
    NPT_XmlParser parser;
    NPT_XmlNode* root = NULL;
    
    const char* doc = "<top1></top1><top2></top2>";
    NPT_Result result = parser.Parse(doc, root);
    CHECK(result == NPT_ERROR_XML_MULTIPLE_ROOTS);
    CHECK(root == NULL);
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int argc, char** argv)
{
    // setup debugging
#if defined(_WIN32) && defined(_DEBUG) && !defined(UNDER_CE)
    int flags = _crtDbgFlag       | 
        _CRTDBG_ALLOC_MEM_DF      |
        _CRTDBG_DELAY_FREE_MEM_DF |
        _CRTDBG_CHECK_ALWAYS_DF;

    _CrtSetDbgFlag(flags);
    //AllocConsole();
    //freopen("CONOUT$", "w", stdout);
#endif 

    // check args
    if (argc == 2) {
        TestFile(argv[1]);
        return 0;
    }

    TestRegression();
    TestComments();
    TestCdata();
    TestWhitespace();
    TestAttributes();
    TestAttributeNormalization();
    TestNamespaces();
    TestSerializer();
    TestMakeStandalone();
    TestCanonicalizer();
    TestFinders();
    TestWriter();
    TestBadInput();
    
    return 0;
}
