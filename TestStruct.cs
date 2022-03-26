using System;
using addons.SubDataInspector;

[Serializable]
public struct TestStruct
{
    [CustomExport]
    string testString;
    
    [CustomExport]
    string testString2;

    [CustomExport]
    OtherTestStruct nestedTest;
}

[Serializable]
public struct OtherTestStruct
{
    [CustomExport]
    string testNestedString;
}