using System;
using System.Collections.Generic;
using CsManagedDataEditor;
using Godot;
using ManagedResourceEditor;

[Serializable]
public struct TestStruct
{
    [ExportCustom]
    string testString;

    [ExportCustom]
    Control.SizeFlags testEnum;

    [ExportCustom]
    byte[] byteArray;

    [ExportCustom]
    List<Control.SizeFlags> enumList;

    [ExportCustom]
    TestFlagsEnum testFlags;
    
    [ExportCustom]
    Resource resourceRef;

    [ExportCustom]
    int testInteger;

    [ExportCustom]
    bool toggleField;

    [ExportCustom]
    OtherTestStruct nestedTest;

    [ExportCustom]
    ResourceReference<SomeOtherResource> otherResourceRef;

    public SomeOtherResource OtherResource => otherResourceRef;
}

[Serializable]
public struct OtherTestStruct
{
    [ExportCustom]
    string testNestedString;

    [ExportCustom]
    byte byteField;

    [ExportCustom]
    ushort ushortField;
}

[Flags]
public enum TestFlagsEnum
{
	None = 0,
	A = 1,
	B= 1 <<1,
	C = 1<<2
}