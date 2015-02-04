﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class ActiveStatementTests : RudeEditTestBase
    {
        #region Update

        [Fact]
        public void Update_Inner()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        while (true)
        {
            <AS:1>Foo(2);</AS:1>
        }
    }

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Foo(2);"));
        }

        [Fact]
        public void Update_Leaf()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        while (true)
        {
            <AS:1>Foo(1);</AS:1>
        }
    }

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a + 1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void Update_Leaf_NewCommentAtEndOfActiveStatement()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>//
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void Update_Inner_NewCommentAtEndOfActiveStatement()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>//
    }

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WorkItem(846588)]
        [Fact]
        public void Update_Leaf_Block()
        {
            string src1 = @"
class C : System.IDisposable
{
    public void Dispose() {}

    static void Main(string[] args)
    {
        using (<AS:0>C x = null</AS:0>) {}
    }
}";
            string src2 = @"
class C : System.IDisposable
{
    public void Dispose() {}

    static void Main(string[] args)
    {
        using (C x) <AS:0>{</AS:0>}
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "using (C x)", "using statement"));
        }

        #endregion

        #region Delete in Method Body

        [Fact]
        public void Delete_Inner()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        while (true)
        {
        }
    <AS:1>}</AS:1>

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "{"));
        }

        // TODO (tomat): considering a change
        [Fact]
        public void Delete_Inner_MultipleParents()
        {
            string src1 = @"
class C : IDisposable
{
    unsafe static void Main(string[] args)
    {
        {
            <AS:1>Foo(1);</AS:1>
        }

        if (true)
        {
            <AS:2>Foo(2);</AS:2>
        }
        else
        {
            <AS:3>Foo(3);</AS:3>
        }

        int x = 1;
        switch (x)
        {
            case 1:
            case 2:
                <AS:4>Foo(4);</AS:4>
                break;

            default:
                <AS:5>Foo(5);</AS:5>
                break;
        }

        checked
        {
            <AS:6>Foo(4);</AS:6>
        }

        unchecked
        {
            <AS:7>Foo(7);</AS:7>
        }

        while (true) <AS:8>Foo(8);</AS:8>
    
        do <AS:9>Foo(9);</AS:9> while (true);

        for (int i = 0; i < 10; i++) <AS:10>Foo(10);</AS:10>

        foreach (var i in new[] { 1, 2}) <AS:11>Foo(11);</AS:11>

        using (var z = new C()) <AS:12>Foo(12);</AS:12>

        fixed (char* p = ""s"") <AS:13>Foo(13);</AS:13>

        label: <AS:14>Foo(14);</AS:14>
    }

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}";
            string src2 = @"
class C : IDisposable
{
    unsafe static void Main(string[] args)
    {
        {
        <AS:1>}</AS:1>

        if (true)
        { <AS:2>}</AS:2>
        else
        { <AS:3>}</AS:3>

        int x = 1;
        switch (x)
        {
            case 1:
            case 2:
                <AS:4>break;</AS:4>

            default:
                <AS:5>break;</AS:5>
        }

        checked
        {
        <AS:6>}</AS:6>

        unchecked
        {
        <AS:7>}</AS:7>

        <AS:8>while (true)</AS:8> { }
    
        do { } <AS:9>while (true);</AS:9>

        for (int i = 0; i < 10; <AS:10>i++</AS:10>) { }

        <AS:11>foreach</AS:11> (var i in new[] { 1, 2 }) { }

        using (<AS:12>var z = new C()</AS:12>) { }

        fixed (<AS:13>char* p = ""s""</AS:13>) { }

        label: <AS:14>{</AS:14> }
    }

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "{"),
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "{"),
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "{"),
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "case 2:"),
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "default:"),
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "{"),
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "{"),
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "while (true)"),
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "do"),
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "for (int i = 0; i < 10;        i++        )"),
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "foreach         (var i in new[] { 1, 2 })"),
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "using (       var z = new C()        )"),
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "fixed (       char* p = \"s\"        )"),
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "label"));
        }

        [Fact]
        public void Delete_Leaf1()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
    <AS:0>}</AS:0>
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void Delete_Leaf2()
        {
            string src1 = @"
class C
{
    static void Foo(int a)
    {
        Console.WriteLine(1);
        Console.WriteLine(2);
        <AS:0>Console.WriteLine(3);</AS:0>
        Console.WriteLine(4);
    }
}";
            string src2 = @"
class C
{
    static void Foo(int a)
    {
        Console.WriteLine(1);
        Console.WriteLine(2);

        <AS:0>Console.WriteLine(4);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void Delete_Leaf_InTry()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
        try
        {
            <AS:0>Console.WriteLine(a);</AS:0>
        }
        catch
        {
        }
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
        try
        {
        <AS:0>}</AS:0>
        catch
        {
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void Delete_Leaf_InTry2()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
        try
        {
            try
            {
                <AS:0>Console.WriteLine(a);</AS:0>
            }
            catch
            {
            }
        }
        catch
        {
        }
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
        try
        {
            try
            {
            <AS:0>}</AS:0>
            catch
            {
            }
        }
        catch
        {
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void Delete_Inner_CommentActiveStatement()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        //Foo(1);
    <AS:1>}</AS:1>

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "{"));
        }

        [WorkItem(755959)]
        [Fact]
        public void Delete_Leaf_CommentActiveStatement()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
        //Console.WriteLine(a);
    <AS:0>}</AS:0>
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        #endregion

        #region Constructors

        [WorkItem(740949)]
        [Fact]
        public void Updated_Inner_Constructor()
        {
            string src1 = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        <AS:1>Foo f = new Foo(5);</AS:1>
    }
}

class Foo
{
    int value;
    public Foo(int a)
    {
        <AS:0>this.value = a;</AS:0>
    }
}";
            string src2 = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        <AS:1>Foo f = new Foo(5*2);</AS:1>
    }
}

class Foo
{
    int value;
    public Foo(int a)
    {
        <AS:0>this.value = a;</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Foo f = new Foo(5*2);"));
        }

        [WorkItem(741249)]
        [Fact]
        public void Updated_Leaf_Constructor()
        {
            string src1 = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        <AS:1>Foo f = new Foo(5);</AS:1>
    }
}

class Foo
{
    int value;
    public Foo(int a)
    {
        <AS:0>this.value = a;</AS:0>
    }
}";
            string src2 = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        <AS:1>Foo f = new Foo(5);</AS:1>
    }
}

class Foo
{
    int value;
    public Foo(int a)
    {
        <AS:0>this.value = a*2;</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WorkItem(742334)]
        [Fact]
        public void Updated_Leaf_Constructor_Parameter()
        {
            string src1 = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        <AS:1>Foo f = new Foo(5);</AS:1>
    }
}

class Foo
{
    int value;
    <AS:0>public Foo(int a)</AS:0>
    {
        this.value = a;
    }
}";
            string src2 = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        <AS:1>Foo f = new Foo(5);</AS:1>
    }
}

class Foo
{
    int value;
    <AS:0>public Foo(int b)</AS:0>
    {
        this.value = b;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.Renamed, "int b", "parameter"));
        }

        [WorkItem(742334)]
        [Fact]
        public void Updated_Leaf_Constructor_Parameter_DefaultValue()
        {
            string src1 = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        <AS:1>Foo f = new Foo(5);</AS:1>
    }
}

class Foo
{
    int value;
    <AS:0>public Foo(int a = 5)</AS:0>
    {
        this.value = a;
    }
}";
            string src2 = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        <AS:1>Foo f = new Foo(5);</AS:1>
    }
}

class Foo
{
    int value;
    <AS:0>public Foo(int a = 42)</AS:0>
    {
        this.value = a;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.InitializerUpdate, "int a = 42", "parameter"));
        }

        [WorkItem(742334)]
        [Fact]
        public void Updated_Leaf_ConstructorChaining1()
        {
            string src1 = @"
using System;

class Test
{
    static void Main(string[] args)
    {
       <AS:1>B b = new B(2, 3);</AS:1>
    }
}
class B : A
{
    public B(int x, int y) : <AS:0>base(x + y, x - y)</AS:0> { }
}
class A
{
    public A(int x, int y) : this(5 + x, y, 0) { }

    public A(int x, int y, int z) { }
}";
            string src2 = @"
using System;

class Test
{
    static void Main(string[] args)
    {
       <AS:1>B b = new B(2, 3);</AS:1>
    }
}
class B : A
{
    public B(int x, int y) : <AS:0>base(x + y + 5, x - y)</AS:0> { }
}
class A
{
    public A(int x, int y) : this(x, y, 0) { }

    public A(int x, int y, int z) { }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WorkItem(742334)]
        [Fact]
        public void Updated_Leaf_ConstructorChaining2()
        {
            string src1 = @"
using System;

class Test
{
    static void Main(string[] args)
    {
       <AS:2>B b = new B(2, 3);</AS:2>
    }
}
class B : A
{
    public B(int x, int y) : <AS:1>base(x + y, x - y)</AS:1> { }
}
class A
{
    public A(int x, int y) : <AS:0>this(x, y, 0)</AS:0> { }

    public A(int x, int y, int z) { }
}";
            string src2 = @"
using System;

class Test
{
    static void Main(string[] args)
    {
       <AS:2>B b = new B(2, 3);</AS:2>
    }
}
class B : A
{
    public B(int x, int y) : <AS:1>base(x + y, x - y)</AS:1> { }
}
class A
{
    public A(int x, int y) : <AS:0>this(5 + x, y, 0)</AS:0> { }

    public A(int x, int y, int z) { }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WorkItem(742334)]
        [Fact]
        public void InstanceConstructorWithoutInitializer()
        {
            string src1 = @"
class C
{
    int a = 5;

    <AS:0>public C(int a)</AS:0> { }

    static void Main(string[] args)
    {
        <AS:1>C c = new C(3);</AS:1>
    }
}";
            string src2 = @"
class C
{
    int a = 42;

    <AS:0>public C(int a)</AS:0> { }

    static void Main(string[] args)
    {
        <AS:1>C c = new C(3);</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void InstanceConstructorWithInitializer_Internal_Update1()
        {
            string src1 = @"
class D
{
    public D(int d) {}
}

class C : D
{
    int a = 5;

    public C(int a) : <AS:2>this(true)</AS:2> { }

    public C(bool b) : <AS:1>base(F())</AS:1> {}

    static int F()
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main(string[] args)
    {
        <AS:3>C c = new C(3);</AS:3>
    }
}";
            string src2 = @"
class D
{
    public D(int d) {}
}

class C : D
{
    int a = 5;

    public C(int a) : <AS:2>this(false)</AS:2> { }

    public C(bool b) : <AS:1>base(F())</AS:1> {}

    static int F()
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main(string[] args)
    {
        <AS:3>C c = new C(3);</AS:3>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "this(false)"));
        }

        [Fact]
        public void InstanceConstructorWithInitializer_Internal_Update2()
        {
            string src1 = @"
class D
{
    public D(int d) {}
}

class C : D
{
    public C() : <AS:1>base(F())</AS:1> {}

    static int F()
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            string src2 = @"
class D
{
    public D(int d) {}
}

class C : D
{
    <AS:1>public C()</AS:1> {}

    static int F()
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "public C()"));
        }

        [Fact]
        public void InstanceConstructorWithInitializer_Internal_Update3()
        {
            string src1 = @"
class D
{
    public D(int d) <AS:0>{</AS:0> }
}

class C : D
{
    <AS:1>public C()</AS:1> {}

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            string src2 = @"
class D
{
    public D(int d) <AS:0>{</AS:0> }
}

class C : D
{
    public C() : <AS:1>base(1)</AS:1> {}

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "base(1)"));
        }

        [Fact]
        public void InstanceConstructorWithInitializer_Leaf_Update1()
        {
            string src1 = @"
class D
{
    public D(int d) { }
}

class C : D
{
    public C() : <AS:0>base(1)</AS:0> {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            string src2 = @"
class D
{
    public D(int d) { }
}

class C : D
{
    public C() : <AS:0>base(2)</AS:0> {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void InstanceConstructorWithInitializer_Leaf_Update2()
        {
            string src1 = @"
class D
{
    public D() { }
    public D(int d) { }
}

class C : D
{
    public C() : <AS:0>base(2)</AS:0> {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            string src2 = @"
class D
{
    public D() { }
    public D(int d) { }
}

class C : D
{
    <AS:0>public C()</AS:0> {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void InstanceConstructorWithInitializer_Leaf_Update3()
        {
            string src1 = @"
class D
{
    public D() { }
    public D(int d) { }
}

class C : D
{
    <AS:0>public C()</AS:0> {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            string src2 = @"
class D
{
    public D() { }
    public D(int d) { }
}

class C : D
{
    public C() : <AS:0>base(2)</AS:0> {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        #endregion

        #region Field and Property Initializers

        [Fact]
        public void InstancePropertyInitializer_Leaf_Update()
        {
            string src1 = @"
class C
{
    int a { get; } = <AS:0>1</AS:0>;

    public C() {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            string src2 = @"
class C
{
    int a { get; } = <AS:0>2</AS:0>;

    public C() {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WorkItem(742334)]
        [Fact]
        public void InstanceFieldInitializer_Leaf_Update1()
        {
            string src1 = @"
class C
{
    <AS:0>int a = 1</AS:0>, b = 2;

    public C() {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            string src2 = @"
class C
{
    <AS:0>int a = 2</AS:0>, b = 2;

    public C() {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void InstanceFieldInitializer_Internal_Update1()
        {
            string src1 = @"
class C
{
    <AS:1>int a = F(1)</AS:1>, b = F(2);

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            string src2 = @"
class C
{
    <AS:1>int a = F(2)</AS:1>, b = F(2);

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "int a = F(2)"));
        }

        [Fact]
        public void InstanceFieldInitializer_Internal_Update2()
        {
            string src1 = @"
class C
{
    int a = F(1), <AS:1>b = F(2)</AS:1>;

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            string src2 = @"
class C
{
    int a = F(1), <AS:1>b = F(3)</AS:1>;

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "b = F(3)"));
        }

        [Fact]
        public void InstancePropertyInitializer_Internal_Delete1()
        {
            string src1 = @"
class C
{
    int a { get; } = <AS:0>1</AS:0>;
    int b { get; } = 2;

    public C() {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            string src2 = @"
class C
{
    int a { get { return 1; } }
    int b { get; } = <AS:0>2</AS:0>;

    public C() { }

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.MethodBodyAdd, "get", "property getter"));
        }

        [Fact]
        public void InstancePropertyInitializer_Internal_Delete2()
        {
            string src1 = @"
class C
{
    int a { get; } = <AS:0>1</AS:0>;
    static int s { get; } = 2;
    int b { get; } = 2;

    public C() {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            string src2 = @"
class C
{
    int a { get; }
    static int s { get; } = 2;
    int b { get; } = <AS:0>3</AS:0>;

    public C() { }

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void InstanceFieldInitializer_Internal_Delete1()
        {
            string src1 = @"
class C
{
    <AS:1>int a = F(1)</AS:1>, b = F(2);

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            string src2 = @"
class C
{
    int a, <AS:1>b = F(2)</AS:1>;

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void InstanceFieldInitializer_Internal_Delete2()
        {
            string src1 = @"
class C
{
    int a = F(1), <AS:1>b = F(2)</AS:1>;

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            string src2 = @"
class C
{
    <AS:1>int a, b;</AS:1>

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void InstancePropertyAndFieldInitializers_Delete1()
        {
            string src1 = @"
class C
{
    int a { get; } = <AS:0>1</AS:0>;
    static int s { get; } = 2;
    int b = 2;

    public C() {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            string src2 = @"
class C
{
    int a { get; }
    static int s { get; } = 2;
    <AS:0>int b = 3;</AS:0>

    public C() { }

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void InstancePropertyAndFieldInitializers_Delete2()
        {
            string src1 = @"
class C
{
    int a = <AS:0>1</AS:0>;
    static int s { get; } = 2;
    int b { get; } = 2;

    public C() {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            string src2 = @"
class C
{
    int a;
    static int s { get; } = 2;
    int b { get; } = <AS:0>3</AS:0>;

    public C() { }

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void InstanceFieldInitializer_SingleDeclarator()
        {
            string src1 = @"
class C
{
    <AS:1>public static readonly int a = F(1);</AS:1>

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            string src2 = @"
class C
{
    <AS:1>public static readonly int a = F(1);</AS:1>

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a + 1;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_Lambda1()
        {
            string src1 = @"
class C
{
    Func<int, int> a = z => <AS:0>z + 1</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>new C().a(1);</AS:1>
    }
}";
            string src2 = @"
class C
{
    Func<int, int> a = F(z => <AS:0>z + 1</AS:0>);

    static void Main(string[] args)
    {
        <AS:1>new C().a(1);</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "z", "field"));
        }

        [Fact]
        public void PropertyInitializer_Lambda1()
        {
            string src1 = @"
class C
{
    Func<int, int> a { get; } = z => <AS:0>z + 1</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>new C().a(1);</AS:1>
    }
}";
            string src2 = @"
class C
{
    Func<int, int> a { get; } = F(z => <AS:0>z + 1</AS:0>);

    static void Main(string[] args)
    {
        <AS:1>new C().a(1);</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "z", "auto-property"));
        }

        [Fact]
        public void FieldInitializer_Lambda2()
        {
            string src1 = @"
class C
{
    Func<int, Func<int>> a = z => () => <AS:0>z + 1</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>new C().a(1)();</AS:1>
    }
}";
            string src2 = @"
class C
{
    Func<int, Func<int>> a = z => () => <AS:0>z + 2</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>new C().a(1)();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "z", "field"));
        }

        [Fact]
        public void PropertyInitializer_Lambda2()
        {
            string src1 = @"
class C
{
    Func<int, Func<int>> a { get; } = z => () => <AS:0>z + 1</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>new C().a(1)();</AS:1>
    }
}";
            string src2 = @"
class C
{
    Func<int, Func<int>> a { get; } = z => () => <AS:0>z + 2</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>new C().a(1)();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "z", "auto-property"));
        }

        [Fact]
        public void FieldInitializer_InsertConst1()
        {
            string src1 = @"
class C
{
    <AS:0>int a = 1</AS:0>;

    public C() {}
}";
            string src2 = @"
class C
{
    <AS:0>const int a = 1;</AS:0>

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a = 1       ;]@24 -> [const int a = 1;]@24");

            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ModifiersUpdate, "const int a = 1", "field"));
        }

        [Fact]
        public void LocalInitializer_InsertConst1()
        {
            string src1 = @"
class C
{
    public void M()
    {
        <AS:0>int a = 1</AS:0>;
    }
}";
            string src2 = @"
class C
{
    public void M()
    {
        const int a = 1;
    <AS:0>}</AS:0>
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_InsertConst2()
        {
            string src1 = @"
class C
{
    int <AS:0>a = 1</AS:0>, b = 2;

    public C() {}
}";
            string src2 = @"
class C
{
    <AS:0>const int a = 1, b = 2;</AS:0>

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);

            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ModifiersUpdate, "const int a = 1, b = 2", "field"));
        }

        [Fact]
        public void LocalInitializer_InsertConst2()
        {
            string src1 = @"
class C
{
    public void M()
    {
        int <AS:0>a = 1</AS:0>, b = 2;
    }
}";
            string src2 = @"
class C
{
    public void M()
    {
        const int a = 1, b = 2;
    <AS:0>}</AS:0>
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_Delete1()
        {
            string src1 = @"
class C
{
    <AS:0>int a = 1;</AS:0>
    int b = 1;

    public C() {}
}";
            string src2 = @"
class C
{
    int a;
    <AS:0>int b = 1;</AS:0>

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void LocalInitializer_Delete1()
        {
            string src1 = @"
class C
{
      public void M() { <AS:0>int a = 1</AS:0>; }
}";
            string src2 = @"
class C
{
    public void M() { int a; <AS:0>}</AS:0> 
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_Delete2()
        {
            string src1 = @"
class C
{
    int b = 1;
    int c;
    <AS:0>int a = 1;</AS:0>

    public C() {}
}";
            string src2 = @"
class C
{
    <AS:0>int b = 1;</AS:0>
    int c;
    int a;

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void LocalInitializer_Delete2()
        {
            string src1 = @"
class C
{
    public void M() 
    {
        int b = 1;
        int c;
        <AS:0>int a = 1;</AS:0>
    }
}";
            string src2 = @"
class C
{
    public void M()
    { 
        int b = 1;
        int c;
        int a;
    <AS:0>}</AS:0>
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_Delete3()
        {
            string src1 = @"
class C
{
    int b = 1;
    int c;
    <AS:0>int a = 1;</AS:0>

    public C() {}
}";
            string src2 = @"
class C
{
    <AS:0>int b = 1;</AS:0>
    int c;

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.Delete, "class C", "field"));
        }

        [Fact]
        public void LocalInitializer_Delete3()
        {
            string src1 = @"
class C
{
    public void M() 
    {
        int b = 1;
        int c;
        <AS:0>int a = 1;</AS:0>
    }
}";
            string src2 = @"
class C
{
    public void M()
    { 
        int b = 1;
        int c;
    <AS:0>}</AS:0>
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_DeleteStaticInstance1()
        {
            string src1 = @"
class C
{
    <AS:0>int a = 1;</AS:0>
    static int b = 1;
    int c = 1;
    
    public C() {}
}";
            string src2 = @"
class C
{
    int a;
    static int b = 1;
    <AS:0>int c = 1;</AS:0>

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_DeleteStaticInstance2()
        {
            string src1 = @"
class C
{
    static int c = 1;
    <AS:0>static int a = 1;</AS:0>
    int b = 1;
    
    public C() {}
}";
            string src2 = @"
class C
{
    <AS:0>static int c = 1;</AS:0>
    static int a;
    int b = 1;

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_DeleteStaticInstance3()
        {
            string src1 = @"
class C
{
    <AS:0>static int a = 1;</AS:0>
    int b = 1;
    
    public C() {}
}";
            string src2 = @"
class C
{
    <AS:0>static int a;</AS:0>
    int b = 1;

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_DeleteMove1()
        {
            string src1 = @"
class C
{
    int b = 1;
    int c;
    <AS:0>int a = 1;</AS:0>

    public C() {}
}";
            string src2 = @"
class C
{
    int c;
    <AS:0>int b = 1;</AS:0>

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.Move, "int c", "field"),
                Diagnostic(RudeEditKind.Delete, "class C", "field"));
        }

        [Fact]
        public void LocalInitializer_DeleteReorder1()
        {
            string src1 = @"
class C
{
    public void M() 
    {
        int b = 1;
        <AS:0>int a = 1;</AS:0>
        int c;
    }
}";
            string src2 = @"
class C
{
    public void M()
    { 
        int c;
        <AS:0>int b = 1;</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void FieldToProperty1()
        {
            string src1 = @"
class C
{
    int a = <AS:0>1</AS:0>;
}";

            // The placement of the active statement is not ideal, but acceptable.
            string src2 = @"
<AS:0>class C</AS:0>
{
    int a { get; } = 1;
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.Delete, "class C", "field"));
        }

        [Fact]
        public void PropertyToField1()
        {
            string src1 = @"
class C
{
    int a { get; } = <AS:0>1</AS:0>;
}";

            // The placement of the active statement is not ideal, but acceptable.
            string src2 = @"
<AS:0>class C</AS:0>
{
    int a = 1;
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.Delete, "class C", "auto-property"));
        }

        #endregion

        #region Lock Statement

        [Fact]
        public void LockBody_Update()
        {
            string src1 = @"
class Test
{
    private static object F() { <AS:0>return new object();</AS:0> }

    static void Main(string[] args)
    {
        <AS:1>lock (F())</AS:1>
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static object F() { <AS:0>return new object();</AS:0> }

    static void Main(string[] args)
    {
        <AS:1>lock (F())</AS:1>
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WorkItem(755749)]
        [Fact]
        public void Lock_Insert_Leaf()
        {
            string src1 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        <AS:0>System.Console.Write(5);</AS:0>
    }
}";
            string src2 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        lock (lockThis)
        {
            <AS:0>System.Console.Write(5);</AS:0>
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "lock (lockThis)", "lock statement"));
        }

        [WorkItem(755749)]
        [Fact]
        public void Lock_Insert_Leaf2()
        {
            string src1 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        {
            System.Console.Write(5);
        <AS:0>}</AS:0>
    }
}";
            string src2 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        lock (lockThis)
        {
            System.Console.Write(5);
        <AS:0>}</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "lock (lockThis)", "lock statement"));
        }

        [Fact]
        public void Lock_Insert_Leaf3()
        {
            string src1 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        {
            System.Console.Write(5);
        }
        <AS:0>System.Console.Write(10);</AS:0>
    }
}";
            string src2 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        lock (lockThis)
        {
            System.Console.Write(5);
        }
        <AS:0>System.Console.Write(5);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void Lock_Insert_Leaf4()
        {
            string src1 = @"
class Test
{
    public static object a = new object();
    public static object b = new object();
    public static object c = new object();
    public static object d = new object();
    public static object e = new object();
    
    static void Main(string[] args)
    {
        lock (a)
        {
            lock (b)
            {
                lock (c)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            string src2 = @"
class Test
{
    public static object a = new object();
    public static object b = new object();
    public static object c = new object();
    public static object d = new object();
    public static object e = new object();
    
    static void Main(string[] args)
    {
        lock (b)
        {
            lock (d)
            {
                lock (a)
                {
                    lock (e)
                    {
                        <AS:0>System.Console.Write();</AS:0>
                    }
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "lock (d)", "lock statement"),
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "lock (e)", "lock statement"));
        }

        [Fact]
        public void Lock_Insert_Leaf5()
        {
            string src1 = @"
class Test
{
    public static object a = new object();
    public static object b = new object();
    public static object c = new object();
    public static object d = new object();
    public static object e = new object();
    
    static void Main(string[] args)
    {
        lock (a)
        {
            lock (c)
            {
                lock (b)
                {
                    lock (e)
                    {
                        <AS:0>System.Console.Write();</AS:0>
                    }
                }
            }
        }
    }
}";

            string src2 = @"
class Test
{
    public static object a = new object();
    public static object b = new object();
    public static object c = new object();
    public static object d = new object();
    public static object e = new object();
    
    static void Main(string[] args)
    {
        lock (b)
        {
            lock (d)
            {
                lock (a)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "lock (d)", "lock statement"));
        }

        [WorkItem(755752)]
        [Fact]
        public void Lock_Update_Leaf()
        {
            string src1 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        lock (lockThis)
        {
            <AS:0>System.Console.Write(5);</AS:0>
        }
    }
}";
            string src2 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        lock (""test"")
        {
            <AS:0>System.Console.Write(5);</AS:0>
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "lock (\"test\")", "lock statement"));
        }

        [Fact]
        public void Lock_Update_Leaf2()
        {
            string src1 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        lock (lockThis)
        {
            System.Console.Write(5);
        }
        <AS:0>System.Console.Write(5);</AS:0>
    }
}";
            string src2 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        lock (""test"")
        {
            System.Console.Write(5);
        }
        <AS:0>System.Console.Write(5);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void Lock_Delete_Leaf()
        {
            string src1 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        lock (lockThis)
        {
            <AS:0>System.Console.Write(5);</AS:0>
        }
    }
}";
            string src2 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        <AS:0>System.Console.Write(5);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        #endregion

        #region Fixed Statement

        [Fact]
        public void FixedBody_Update()
        {
            string src1 = @"
class Test
{
    private static string F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        unsafe
        {
            char* px2;
            fixed (<AS:1>char* pj = &F()</AS:1>)
            {
                System.Console.WriteLine(0);
            }
        }
    }
}";
            string src2 = @"
class Test
{
    private static string F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        unsafe
        {
            char* px2;
            fixed (<AS:1>char* pj = &F()</AS:1>)
            {
                System.Console.WriteLine(1);
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WorkItem(755742)]
        [Fact]
        public void Fixed_Insert_Leaf()
        {
            string src1 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            <AS:0>px2 = null;</AS:0>
        }
    }
}";
            string src2 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* pj = &value)
            {
                <AS:0>px2 = null;</AS:0>
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "fixed (int* pj = &value)", "fixed statement"));
        }

        [Fact]
        public void Fixed_Insert_Leaf2()
        {
            string src1 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
        <AS:0>}</AS:0>
    }
}";
            string src2 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* pj = &value)
            {
                px2 = null;
            }
        <AS:0>}</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WorkItem(755742)]
        [Fact]
        public void Fixed_Insert_Leaf3()
        {
            string src1 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            <AS:0>px2 = null;</AS:0>

            fixed (int* pj = &value)
            {
                
            }
        }
    }
}";
            string src2 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* pj = &value)
            {
                <AS:0>px2 = null;</AS:0>
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "fixed (int* pj = &value)", "fixed statement"));
        }

        [WorkItem(755742)]
        [Fact]
        public void Fixed_Reorder_Leaf1()
        {
            string src1 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* a = &value)
            {
                fixed (int* b = &value)
                {
                    <AS:0>px2 = null;</AS:0>
                }
            }
        }
    }
}";
            string src2 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* b = &value)
            {
                fixed (int* a = &value)
                {
                    <AS:0>px2 = null;</AS:0>
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WorkItem(755746)]
        [Fact]
        public void Fixed_Update_Leaf1()
        {
            string src1 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* pj = &value)
            {
                <AS:0>px2 = null;</AS:0>
            }
        }
    }
}";
            string src2 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* p = &value)
            {
                <AS:0>px2 = null;</AS:0>
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "fixed (int* p = &value)", "fixed statement"));
        }

        [WorkItem(755746)]
        [Fact]
        public void Fixed_Update_Leaf2()
        {
            string src1 = @"
class Test
{
    public static int value1 = 10;
    public static int value2 = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* a = &value1)
            {
                fixed (int* b = &value1)
                {
                    fixed (int* c = &value1)
                    {
                        <AS:0>px2 = null;</AS:0>
                    }
                }
            }
        }
    }
}";
            string src2 = @"
class Test
{
    public static int value1 = 10;
    public static int value2 = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* c = &value1)
            {
                fixed (int* d = &value1)
                {
                    fixed (int* a = &value2)
                    {
                        fixed (int* e = &value1)
                        {
                            <AS:0>px2 = null;</AS:0>
                        }
                    }
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "fixed (int* a = &value2)", "fixed statement"),
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "fixed (int* d = &value1)", "fixed statement"),
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "fixed (int* e = &value1)", "fixed statement"));
        }

        [Fact]
        public void Fixed_Delete_Leaf()
        {
            string src1 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* pj = &value)
            {
                <AS:0>px2 = null;</AS:0>
            }
        }
    }
}";
            string src2 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            <AS:0>px2 = null;</AS:0>
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        #endregion

        #region ForEach Statement

        [Fact]
        public void ForEachBody_Update_ExpressionActive()
        {
            string src1 = @"
class Test
{
    private static string F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (char c in <AS:1>F()</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static string F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (char c in <AS:1>F()</AS:1>)
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void ForEachBody_Update_InKeywordActive()
        {
            string src1 = @"
class Test
{
    private static string F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (char c <AS:1>in</AS:1> F())
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static string F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (char c <AS:1>in</AS:1> F())
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void ForEachBody_Update_VariableActive()
        {
            string src1 = @"
class Test
{
    private static string[] F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (<AS:1>string c</AS:1> in F())
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static string[] F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (<AS:1>string c</AS:1> in F())
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void ForEachBody_Update_ForeachKeywordActive()
        {
            string src1 = @"
class Test
{
    private static string F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        <AS:1>foreach</AS:1> (char c in F())
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static string F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        <AS:1>foreach</AS:1> (char c in F())
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void ForEachVariable_Update()
        {
            string src1 = @"
class Test
{
    private static string[] F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (<AS:1>string c</AS:1> in F())
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static string[] F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (<AS:1>object c</AS:1> in F())
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // not ideal, but good enough:
            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "object c"),
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "foreach (      object c        in F())", "foreach statement"));
        }

        [Fact]
        public void ForEach_Reorder_Leaf1()
        {
            string src1 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var a in e1)
        {
            foreach (var b in e1)
            {
                foreach (var c in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            string src2 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var b in e1)
        {
            foreach (var c in e1)
            {
                foreach (var a in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void ForEach_Update_Leaf1()
        {
            string src1 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var a in e1)
        {
            foreach (var b in e1)
            {
                foreach (var c in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            string src2 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var b in e1)
        {
            foreach (var c in e1)
            {
                foreach (var a in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void ForEach_Update_Leaf2()
        {
            string src1 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        <AS:0>System.Console.Write();</AS:0>
    }
}";
            string src2 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var b in e1)
        {
            foreach (var c in e1)
            {
                foreach (var a in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "foreach (var b in e1)", "foreach statement"),
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "foreach (var c in e1)", "foreach statement"),
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "foreach (var a in e1)", "foreach statement"));
        }

        [Fact]
        public void ForEach_Delete_Leaf1()
        {
            string src1 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var a in e1)
        {
            foreach (var b in e1)
            {
                foreach (var c in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            string src2 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var a in e1)
        {
            foreach (var b in e1)
            {
                <AS:0>System.Console.Write();</AS:0>
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void ForEach_Delete_Leaf2()
        {
            string src1 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var a in e1)
        {
            foreach (var b in e1)
            {
                foreach (var c in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            string src2 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var b in e1)
        {
            foreach (var c in e1)
            {
                <AS:0>System.Console.Write();</AS:0>
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void ForEach_Delete_Leaf3()
        {
            string src1 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var a in e1)
        {
            foreach (var b in e1)
            {
                foreach (var c in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            string src2 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var a in e1)
        {
            foreach (var c in e1)
            {
                <AS:0>System.Console.Write();</AS:0>
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void ForEach_Lambda1()
        {
            string src1 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        Action a = () =>
        {
            <AS:0>System.Console.Write();</AS:0>
        };

        <AS:1>a();</AS:1>
    }
}";
            string src2 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var b in e1)
        {
            foreach (var c in e1)
            {
                Action a = () =>
                {                
                    foreach (var a in e1)
                    {
                        <AS:0>System.Console.Write();</AS:0>
                    }
                };
            }

            <AS:1>a();</AS:1>
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "foreach (var a in e1)", "foreach statement"),
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "foreach (var b in e1)", "foreach statement"),
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "()", "method"));
        }

        #endregion

        #region For Statement

        [Fact]
        public void ForStatement_Initializer1()
        {
            string src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        int i;
        for (<AS:1>i = F(1)</AS:1>; i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        int i;
        for (<AS:1>i = F(2)</AS:1>; i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "i = F(2)"));
        }

        [Fact]
        public void ForStatement_Initializer2()
        {
            string src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        int i;
        for (<AS:1>i = F(1)</AS:1>, F(1); i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        int i;
        for (<AS:1>i = F(1)</AS:1>, F(2); i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void ForStatement_Initializer_Delete()
        {
            string src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        int i;
        for (<AS:1>i = F(1)</AS:1>; i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        int i;
        for (; <AS:1>i < 10</AS:1>; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "for (;       i < 10       ; i++)"));
        }

        [Fact]
        public void ForStatement_Declarator1()
        {
            string src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (<AS:1>var i = F(1)</AS:1>; i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (<AS:1>var i = F(2)</AS:1>; i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "var i = F(2)"));
        }

        [Fact]
        public void ForStatement_Declarator2()
        {
            string src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (<AS:1>int i = F(1)</AS:1>, j = F(1); i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (<AS:1>int i = F(1)</AS:1>, j = F(2); i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void ForStatement_Declarator3()
        {
            string src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (<AS:1>int i = F(1)</AS:1>; i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (<AS:1>int i = F(1)</AS:1>, j = F(2); i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void ForStatement_Condition1()
        {
            string src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; <AS:1>i < F(10)</AS:1>; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; <AS:1>i < F(20)</AS:1>; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "i < F(20)"));
        }

        [Fact]
        public void ForStatement_Condition_Delete()
        {
            string src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; <AS:1>i < F(10)</AS:1>; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; ; <AS:1>i++</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_ACTIVE_STMT_DELETED, "for (int i = 1; ;       i++       )"));
        }

        [Fact]
        public void ForStatement_Incrementors1()
        {
            string src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; i < F(10); <AS:1>F(1)</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; i < F(20); <AS:1>F(1)</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void ForStatement_Incrementors2()
        {
            string src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; i < F(10); <AS:1>F(1)</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; i < F(10); <AS:1>F(2)</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "F(2)"));
        }

        [Fact]
        public void ForStatement_Incrementors3()
        {
            string src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; i < F(10); <AS:1>F(1)</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; i < F(10); <AS:1>F(1)</AS:1>, i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void ForStatement_Incrementors4()
        {
            string src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; i < F(10); <AS:1>F(1)</AS:1>, i++)
        {
            System.Console.Write(0);
        }
    }
}";
            string src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; i < F(10); i++, <AS:1>F(1)</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        #endregion

        #region Using Statement

        [Fact]
        public void Using_Update_Leaf1()
        {
            string src1 = @"
class Test
{
    public static System.IDisposable a = null;
    public static System.IDisposable b = null;
    public static System.IDisposable c = null;
    
    static void Main(string[] args)
    {
        using (a)
        {
            using (b)
            {
                <AS:0>System.Console.Write();</AS:0>
            }
        }
    }
}";
            string src2 = @"
class Test
{
    public static System.IDisposable a = null;
    public static System.IDisposable b = null;
    public static System.IDisposable c = null;
    
    static void Main(string[] args)
    {
        using (a)
        {
            using (c)
            {
                using (b)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "using (c)", "using statement"));
        }

        [Fact]
        public void Using_Lambda1()
        {
            string src1 = @"
class Test
{
    public static System.IDisposable a = null;
    public static System.IDisposable b = null;
    public static System.IDisposable c = null;
    public static System.IDisposable d = null;
    
    static void Main(string[] args)
    {
        using (a)
        {
            Action a = () =>
            {
                using (b)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            };
        }

        <AS:1>a();</AS:1>
    }
}";
            string src2 = @"
class Test
{
    public static System.IDisposable a = null;
    public static System.IDisposable b = null;
    public static System.IDisposable c = null;
    public static System.IDisposable d = null;
    
    static void Main(string[] args)
    {
        using (d)
        {
            Action a = () =>
            {
                using (c)
                {
                    using (b)
                    {
                        <AS:0>System.Console.Write();</AS:0>
                    }
                }
            };
        }

        <AS:1>a();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "using (c)", "using statement"),
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "()", "method"));
        }

        #endregion

        #region Conditional Block Statements (If, Switch, While, Do)

        [Fact]
        public void IfBody_Update1()
        {
            string src1 = @"
class C
{
	public static bool B() <AS:0>{</AS:0> return false; }
	
	public static void Main()
	{
		<AS:1>if (B())</AS:1>
		{
			System.Console.WriteLine(0);
        }
    }
}";
            string src2 = @"
class C
{
	public static bool B() <AS:0>{</AS:0> return false; }
	
	public static void Main()
	{
		<AS:1>if (B())</AS:1>
		{
			System.Console.WriteLine(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void IfBody_Update2()
        {
            string src1 = @"
class C
{
	public static bool B() <AS:0>{</AS:0> return false; }
	
	public static void Main()
	{
		<AS:1>if (B())</AS:1>
		{
			System.Console.WriteLine(0);
        }
    }
}";
            string src2 = @"
class C
{
	public static bool B() <AS:0>{</AS:0> return false; }
	
	public static void Main()
	{
		<AS:1>if (!B())</AS:1>
		{
			System.Console.WriteLine(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "if (!B())"));
        }

        [Fact]
        public void WhileBody_Update1()
        {
            string src1 = @"
class C
{
	public static bool B() <AS:0>{</AS:0> return false; }
	
	public static void Main()
	{
		<AS:1>while (B())</AS:1>
		{
			System.Console.WriteLine(0);
        }
    }
}";
            string src2 = @"
class C
{
	public static bool B() <AS:0>{</AS:0> return false; }
	
	public static void Main()
	{
		<AS:1>while (B())</AS:1>
		{
			System.Console.WriteLine(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void WhileBody_Update2()
        {
            string src1 = @"
class C
{
	public static bool B() <AS:0>{</AS:0> return false; }
	
	public static void Main()
	{
		<AS:1>while (B())</AS:1>
		{
			System.Console.WriteLine(0);
        }
    }
}";
            string src2 = @"
class C
{
	public static bool B() <AS:0>{</AS:0> return false; }
	
	public static void Main()
	{
		<AS:1>while (!B())</AS:1>
		{
			System.Console.WriteLine(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "while (!B())"));
        }

        [Fact]
        public void DoWhileBody_Update1()
        {
            string src1 = @"
class C
{
	public static bool B() <AS:0>{</AS:0> return false; }
	
	public static void Main()
	{
		do
		{
			System.Console.WriteLine(0);
        }
        <AS:1>while (B());</AS:1>
    }
}";
            string src2 = @"
class C
{
	public static bool B() <AS:0>{</AS:0> return false; }
	
	public static void Main()
	{
		do
		{
			System.Console.WriteLine(1);
        }
		<AS:1>while (B());</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void DoWhileBody_Update2()
        {
            string src1 = @"
class C
{
	public static bool B() <AS:0>{</AS:0> return false; }
	
	public static void Main()
	{
		do
		{
			System.Console.WriteLine(0);
        }
        <AS:1>while (B());</AS:1>
    }
}";
            string src2 = @"
class C
{
	public static bool B() <AS:0>{</AS:0> return false; }
	
	public static void Main()
	{
		do
		{
			System.Console.WriteLine(1);
        }
		<AS:1>while (!B());</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "while (!B());"));
        }

        [Fact]
        public void SwitchCase_Update1()
        {
            string src1 = @"
class C
{
	public static string F() <AS:0>{</AS:0> return null; }
	
	public static void Main()
	{
		<AS:1>switch (F())</AS:1>
		{
			case ""a"": System.Console.WriteLine(0); break;
			case ""b"": System.Console.WriteLine(1); break;
        }
    }
}";
            string src2 = @"
class C
{
	public static string F() <AS:0>{</AS:0> return null; }
	
	public static void Main()
	{
		<AS:1>switch (F())</AS:1>
		{
			case ""a"": System.Console.WriteLine(0); break;
			case ""b"": System.Console.WriteLine(2); break;
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        #endregion

        #region Try

        [Fact]
        public void Try_Add_Inner()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            <AS:1>Foo();</AS:1>
        }
        catch 
        {
        }
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "try", "try block"));
        }

        [Fact]
        public void Try_Add_Leaf()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        try
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
        catch
        {
        }
    } 
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void Try_Delete_Inner()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            <AS:1>Foo();</AS:1>
        }
        catch 
        {
        }
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_DELETE_AROUND, "Foo();", "try block"));
        }

        [Fact]
        public void Try_Delete_Leaf()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>        
    }

    static void Foo()
    {
        try
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
        catch
        {
        }
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void Try_Update_Inner()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            <AS:1>Foo();</AS:1>
        }
        catch
        {
        }
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            <AS:1>Foo();</AS:1>
        }
        catch (IOException)
        {
        }
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "try", "try block"));
        }

        [Fact]
        public void Try_Update_Inner2()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            <AS:1>Foo();</AS:1>
        }
        <ER:1.0>catch
        {
        }</ER:1.0>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            <AS:1>Foo();</AS:1>
        }
        <ER:1.0>catch
        {
        }</ER:1.0>
        Console.WriteLine(2);
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void TryFinally_Update_Inner()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            <AS:1>Foo();</AS:1>
        }
        <ER:1.0>finally
        {
        }</ER:1.0>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            <AS:1>Foo();</AS:1>
        }
        <ER:1.0>finally
        {
        }</ER:1.0>
        Console.WriteLine(2);
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void Try_Update_Leaf()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        try
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
        catch
        {
        }
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        try
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
        catch (IOException)
        {
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        #endregion

        #region Catch

        [Fact]
        public void Catch_Add_Inner()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
        }
        catch 
        {
            <AS:1>Foo();</AS:1>
        }
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "catch", "catch clause"));
        }

        [Fact]
        public void Catch_Add_Leaf()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        try
        {
        }
        catch 
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "catch", "catch clause"));
        }

        [Fact]
        public void Catch_Delete_Inner()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
        }
        catch 
        {
            <AS:1>Foo();</AS:1>
        }
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_DELETE_AROUND, "Foo();", "catch clause"));
        }

        [Fact]
        public void Catch_Delete_Leaf()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>        
    }

    static void Foo()
    {
        try
        {
        }
        catch
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_DELETE_AROUND, "Console.WriteLine(1);", "catch clause"));
        }

        [Fact]
        public void Catch_Update_Inner()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
        }
        catch
        {
            <AS:1>Foo();</AS:1>
        }
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
        }
        catch (IOException)
        {
            <AS:1>Foo();</AS:1>
        }
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "catch", "catch clause"));
        }

        [Fact]
        public void Catch_Update_InFilter_Inner()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        catch (IOException) <AS:1>when (Foo(1))</AS:1>
        {
        }
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        catch (Exception) <AS:1>when (Foo(1))</AS:1>
        {
        }
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "catch", "catch clause"));
        }

        [Fact]
        public void Catch_Update_Leaf()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        try
        {
        }
        catch
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        try
        {
        }
        catch (IOException)
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "catch", "catch clause"));
        }

        [Fact]
        public void CatchFilter_Update_Inner()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        catch (IOException) <AS:1>when (Foo(1))</AS:1>
        {
        }
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        catch (IOException) <AS:1>when (Foo(2))</AS:1>
        {
        }
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "when (Foo(2))"),
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "catch", "catch clause"));
        }

        [Fact]
        public void CatchFilter_Update_Leaf1()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        catch (IOException) <AS:0>when (Foo(1))</AS:0>
        {
        }
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        catch (IOException) <AS:0>when (Foo(2))</AS:0>
        {
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "catch", "catch clause"));
        }

        [Fact]
        public void CatchFilter_Update_Leaf2()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        catch (IOException) <AS:0>when (Foo(1))</AS:0>
        {
        }
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        catch (Exception) <AS:0>when (Foo(1))</AS:0>
        {
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "catch", "catch clause"));
        }

        #endregion

        #region Finally

        [Fact]
        public void Finally_Add_Inner()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
        }
        finally 
        {
            <AS:1>Foo();</AS:1>
        }
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "finally", "finally clause"));
        }

        [Fact]
        public void Finally_Add_Leaf()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        try
        {
        }
        finally 
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "finally", "finally clause"));
        }

        [Fact]
        public void Finally_Delete_Inner()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
        }
        finally 
        {
            <AS:1>Foo();</AS:1>
        }
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_DELETE_AROUND, "Foo();", "finally clause"));
        }

        [Fact]
        public void Finally_Delete_Leaf()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>        
    }

    static void Foo()
    {
        try
        {
        }
        finally
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo();</AS:1>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_DELETE_AROUND, "Console.WriteLine(1);", "finally clause"));
        }

        #endregion

        #region Try-Catch-Finally

        [Fact]
        public void TryCatchFinally()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        catch (IOException)
        {
            try
            {
                try
                {
                    try
                    {
                        <AS:1>Foo();</AS:1>
                    }
                    catch 
                    {
                    }
                }
                catch (Exception)
                {
                }
            }
            finally
            {
            }
        }
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
        }
        catch (Exception)
        {
            try
            {
                try
                {
                }
                finally
                {
                    try
                    {
                        <AS:1>Foo();</AS:1>
                    }
                    catch 
                    {
                    }
                }   
            }
            catch (Exception)
            {
            }
        }
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "catch", "catch clause"),
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "try", "try block"),
                Diagnostic(RudeEditKind.RUDE_EDIT_DELETE_AROUND, "Foo();", "try block"),
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "finally", "finally clause"));
        }

        [Fact]
        public void TryCatchFinally_Regions()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException)
        {
            try
            {
                try
                {
                    try
                    {
                        <AS:1>Foo();</AS:1>
                    }
                    catch 
                    {
                    }
                }
                catch (Exception)
                {
                }
            }
            finally
            {
            }
        }</ER:1.0>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException)
        {
            try { try { try { <AS:1>Foo();</AS:1> } catch { } } catch (Exception) { } } finally { }
        }</ER:1.0>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void TryFilter_Regions1()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException e) when (e == null)
        {
            <AS:1>Foo();</AS:1>
        }</ER:1.0>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException e) when (e == null) { <AS:1>Foo();</AS:1> }</ER:1.0>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void TryFilter_Regions2()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException e) <AS:1>when (e == null)</AS:1>
        {
            Foo();
        }</ER:1.0>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException e) <AS:1>when (e == null)</AS:1> { Foo(); }</ER:1.0>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void Try_Lambda1()
        {
            string src1 = @"
using System;
using System.Linq;
class C
{
    static int Foo(int x)
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main()
    {
        Func<int, int> f = null;
        try
        {
            f = x => <AS:1>1 + Foo(x)</AS:1>;
        }
        catch
        {
        }

        <AS:2>Console.Write(f(2));</AS:2>
    }
}";
            string src2 = @"
using System;
using System.Linq;
class C
{
    static int Foo(int x)
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main()
    {
        Func<int, int> f = null;

        f = x => <AS:1>1 + Foo(x)</AS:1>;

        <AS:2>Console.Write(f(2));</AS:2>
    }
}";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "x", "method"));
        }

        [Fact]
        public void Try_Lambda2()
        {
            string src1 = @"
using System;
using System.Linq;
class C
{
    static int Foo(int x)
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main()
    {
        Func<int, int> f = x => 
        {
            try
            {
                <AS:1>return 1 + Foo(x);</AS:1>
            }
            catch
            {
            }
        };

        <AS:2>Console.Write(f(2));</AS:2>
    }
}";
            string src2 = @"
using System;
using System.Linq;
class C
{
    static int Foo(int x)
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main()
    {
        Func<int, int> f = x => 
        {
            <AS:1>return 1 + Foo(x);</AS:1>
        };

        <AS:2>Console.Write(f(2));</AS:2>
    }
}";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_DELETE_AROUND, "return 1 + Foo(x);", "try block"),
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "x", "method"));
        }

        [Fact]
        public void Try_Query_Join1()
        {
            string src1 = @"
class C
{
    static int Foo(int x)
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main()
    {
        try
        {
            q = from x in xs
                join y in ys on <AS:1>F()</AS:1> equals G()
                select 1;
        }
        catch
        {
        }

        <AS:2>q.ToArray();</AS:2>
    }
}";
            string src2 = @"
class C
{
    static int Foo(int x)
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main()
    {
        q = from x in xs
            join y in ys on <AS:1>F()</AS:1> equals G()
            select 1;

        <AS:2>q.ToArray();</AS:2>
    }
}";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_QUERY_EXPRESSION, "from", "method"));
        }

        #endregion

        #region Checked/Unchecked

        [Fact]
        public void CheckedUnchecked_Insert_Leaf()
        {
            string src1 = @"
class Test
{
    static void Main(string[] args)
    {
        int a = 1, b = 2;
        <AS:0>Console.WriteLine(a*b);</AS:0>
    }
}";
            string src2 = @"
class Test
{
    static void Main(string[] args)
    {
        int a = 1, b = 2;
        checked
        {
            <AS:0>Console.WriteLine(a*b);</AS:0>
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void CheckedUnchecked_Insert_Internal()
        {
            string src1 = @"
class Test
{
    static void Main(string[] args)
    {
        <AS:1>System.Console.WriteLine(5 * M(1, 2));</AS:1>
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            string src2 = @"
class Test
{
    static void Main(string[] args)
    {
        checked
        {
            <AS:1>System.Console.WriteLine(5 * M(1, 2));</AS:1>
        }
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "checked", "checked statement"));
        }

        [Fact]
        public void CheckedUnchecked_Delete_Internal()
        {
            string src1 = @"
class Test
{
    static void Main(string[] args)
    {
        checked
        {
            <AS:1>System.Console.WriteLine(5 * M(1, 2));</AS:1>
        }
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            string src2 = @"
class Test
{
    static void Main(string[] args)
    {
        <AS:1>System.Console.WriteLine(5 * M(1, 2));</AS:1>
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_DELETE_AROUND, "System.Console.WriteLine(5 * M(1, 2));", "checked statement"));
        }

        [Fact]
        public void CheckedUnchecked_Update_Internal()
        {
            string src1 = @"
class Test
{
    static void Main(string[] args)
    {
        unchecked
        {   
            <AS:1>System.Console.WriteLine(5 * M(1, 2));</AS:1>
        }
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            string src2 = @"
class Test
{
    static void Main(string[] args)
    {
        checked
        {
            <AS:1>System.Console.WriteLine(5 * M(1, 2));</AS:1>
        }
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "checked", "checked statement"));
        }

        [Fact]
        public void CheckedUnchecked_Lambda1()
        {
            string src1 = @"
class Test
{
    static void Main(string[] args)
    {
        unchecked
        {   
            Action f = () => <AS:1>5 * M(1, 2)</AS:1>;
        }

        <AS:2>f();</AS:2>
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            string src2 = @"
class Test
{
    static void Main(string[] args)
    {
        checked
        {   
            Action f = () => <AS:1>5 * M(1, 2)</AS:1>;
        }

        <AS:2>f();</AS:2>
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "checked", "checked statement"),
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "()", "method"));
        }

        [Fact]
        public void CheckedUnchecked_Query1()
        {
            string src1 = @"
using System.Collections.Generic;
using System.Linq;

class Test
{
    static void Main()
    {
        IEnumerable<int> f;
        unchecked
        {
            f = from a in new[] { 5 } select <AS:1>M(a, int.MaxValue)</AS:1>;
        }

        <AS:2>f.ToArray();</AS:2>
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            string src2 = @"
using System.Collections.Generic;
using System.Linq;

class Test
{
    static void Main()
    {
        IEnumerable<int> f;
        checked
        {
            f = from a in new[] { 5 } select <AS:1>M(a, int.MaxValue)</AS:1>;
        }

        <AS:2>f.ToArray();</AS:2>
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_AROUND_ACTIVE_STMT, "checked", "checked statement"),
                Diagnostic(RudeEditKind.RUDE_EDIT_QUERY_EXPRESSION, "from", "method"));
        }

        #endregion

        #region Lambdas

        [Fact]
        public void Lambdas_ExpressionToStatements()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, int> f = a => <AS:0>1</AS:0>;
    }
}
";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:0>Func<int, int> f = a => { return 1; };</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "a", "method"));
        }

        [Fact]
        public void Lambdas_ExpressionToDelegate()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, int> f = a => <AS:0>1</AS:0>;
    }
}
";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:0>Func<int, int> f = delegate(int a) { return 1; };</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_ANON_METHOD, "delegate", "method"));
        }

        [Fact]
        public void Lambdas_StatementsToExpression()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, int> f = a => { <AS:0>return 1;</AS:0> };
    }
}
";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:0>Func<int, int> f = a => 1;</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "a", "method"));
        }

        [Fact]
        public void Lambdas_DelegateToExpression()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, int> f = delegate(int a) { <AS:0>return 1;</AS:0> };
    }
}
";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:0>Func<int, int> f = a => 1;</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "a", "method"));
        }

        [Fact]
        public void Lambdas_StatementsToDelegate()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, int> f = a => { <AS:0>return 1;</AS:0> };
    }
}
";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, int> f = delegate(int a) { <AS:0>return 2;</AS:0> };
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // TODO (bug 755959): better deleted active statement span
            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_ANON_METHOD, "delegate", "method"));
        }

        [Fact]
        public void Lambdas_ActiveStatementRemoved1()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, Func<int, int>> f = a =>
        {
            return b =>
            {
                <AS:0>return b;</AS:0>
            };
        };

        var z = f(1);
        <AS:1>z(2);</AS:1>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, int> f = b =>
        {
            <AS:0>return b;</AS:0>
        };

        var z = f;
        <AS:1>z(2);</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "return b;", "lambda"),
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "b", "method"));
        }

        [Fact]
        public void Lambdas_ActiveStatementRemoved2()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, Func<int, int>> f = a => (b) => <AS:0>b</AS:0>;

        var z = f(1);
        <AS:1>z(2);</AS:1>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, int> f = <AS:0>(b)</AS:0> => b;

        var z = f;
        <AS:1>z(2);</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "(b)", "lambda"),
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "(b)", "method"));
        }

        [Fact]
        public void Lambdas_ActiveStatementRemoved3()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, Func<int, int>> f = a =>
        {
            Func<int, int> z;

            F(b =>
            {
                <AS:0>return b;</AS:0>
            }, out z);

            return z;
        };

        var z = f(1);
        <AS:1>z(2);</AS:1>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, int> f = b =>
        {
            <AS:0>F(b);</AS:0>

            return 1;
        };

        var z = f;
        <AS:1>z(2);</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "F(b);", "lambda"),
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "b", "method"));
        }

        [Fact]
        public void Queries_ActiveStatementRemoved_WhereClause()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        var s = from a in b where <AS:0>b.foo</AS:0> select b.bar;
        <AS:1>s.ToArray();</AS:1>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        var <AS:0>s = from a in b select b.bar</AS:0>;
        <AS:1>s.ToArray();</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "s = from a in b select b.bar", "where clause"),
                Diagnostic(RudeEditKind.RUDE_EDIT_QUERY_EXPRESSION, "from", "method"));
        }

        [Fact]
        public void Queries_ActiveStatementRemoved_LetClause()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        var s = from a in b let x = <AS:0>b.foo</AS:0> select x;
        <AS:1>s.ToArray();</AS:1>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        var <AS:0>s = from a in b select a.bar</AS:0>;
        <AS:1>s.ToArray();</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "s = from a in b select a.bar", "let clause"),
                Diagnostic(RudeEditKind.RUDE_EDIT_QUERY_EXPRESSION, "from", "method"));
        }

        [Fact]
        public void Queries_ActiveStatementRemoved_JoinClauseLeft()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        var s = from a in b
                join c in d on <AS:0>a.foo</AS:0> equals c.bar
                select a.bar;

        <AS:1>s.ToArray();</AS:1>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        var <AS:0>s = from a in b select a.bar</AS:0>;
        <AS:1>s.ToArray();</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "s = from a in b select a.bar", "join clause"),
                Diagnostic(RudeEditKind.RUDE_EDIT_QUERY_EXPRESSION, "from", "method"));
        }

        [Fact]
        public void Queries_ActiveStatementRemoved_OrderBy1()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        var s = from a in b
                orderby <AS:0>a.x</AS:0>, a.y descending, a.z ascending
                select a;

        <AS:1>s.ToArray();</AS:1>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        var <AS:0>s = from a in b select a.bar</AS:0>;
        <AS:1>s.ToArray();</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "s = from a in b select a.bar", "orderby clause"),
                Diagnostic(RudeEditKind.RUDE_EDIT_QUERY_EXPRESSION, "from", "method"));
        }

        [Fact]
        public void Queries_ActiveStatementRemoved_OrderBy2()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        var s = from a in b
                orderby a.x, <AS:0>a.y</AS:0> descending, a.z ascending
                select a;

        <AS:1>s.ToArray();</AS:1>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        var <AS:0>s = from a in b select a.bar</AS:0>;
        <AS:1>s.ToArray();</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "s = from a in b select a.bar", "orderby clause"),
                Diagnostic(RudeEditKind.RUDE_EDIT_QUERY_EXPRESSION, "from", "method"));
        }

        [Fact]
        public void Queries_ActiveStatementRemoved_OrderBy3()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        var s = from a in b
                orderby a.x, a.y descending, <AS:0>a.z</AS:0> ascending
                select a;

        <AS:1>s.ToArray();</AS:1>
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        var <AS:0>s = from a in b select a.bar</AS:0>;
        <AS:1>s.ToArray();</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "s = from a in b select a.bar", "orderby clause"),
                Diagnostic(RudeEditKind.RUDE_EDIT_QUERY_EXPRESSION, "from", "method"));
        }

        #endregion

        #region State Machines

        [Fact]
        public void MethodToIteratorMethod_WithActiveStatement()
        {
            string src1 = @"
class C
{
    static IEnumerable<int> F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        return new[] { 1, 2, 3 };
    }
}
";
            string src2 = @"
class C
{
    static IEnumerable<int> F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        yield return 1;
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "yield return 1;", "yield statement"));
        }

        [Fact]
        public void MethodToIteratorMethod_WithActiveStatementInLambda()
        {
            string src1 = @"
class C
{
    static IEnumerable<int> F()
    {
        var f = new Action(() => { <AS:0>Console.WriteLine(1);</AS:0> });
        return new[] { 1, 2, 3 };
    }
}
";
            string src2 = @"
class C
{
    static IEnumerable<int> F()
    {
        var f = new Action(() => { <AS:0>Console.WriteLine(1);</AS:0> });
        yield return 1;
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // should not contain RUDE_EDIT_INSERT_AROUND
            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "()", "method"));
        }

        [Fact]
        public void MethodToIteratorMethod_WithoutActiveStatement()
        {
            string src1 = @"
class C
{
    static IEnumerable<int> F()
    {
        Console.WriteLine(1);
        return new[] { 1, 2, 3 };
    }
}
";
            string src2 = @"
class C
{
    static IEnumerable<int> F()
    {
        Console.WriteLine(1);
        yield return 1;
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [Fact]
        public void MethodToAsyncMethod_WithActiveStatement()
        {
            string src1 = @"
class C
{
    static Task<int> F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>

        return Task.FromResult(1);
    }
}
";
            string src2 = @"
class C
{
    static async Task<int> F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        return await Task.FromResult(1);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_INSERT_AROUND, "await", "await expression"));
        }

        [Fact]
        public void MethodToAsyncMethod_WithActiveStatementInLambda()
        {
            string src1 = @"
class C
{
    static Task<int> F()
    {
        var f = new Action(() => { <AS:0>Console.WriteLine(1);</AS:0> });
        return Task.FromResult(1);
    }
}
";
            string src2 = @"
class C
{
    static async Task<int> F()
    {
        var f = new Action(() => { <AS:0>Console.WriteLine(1);</AS:0> });
        return await Task.FromResult(1);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // should not contain RUDE_EDIT_INSERT_AROUND
            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.RUDE_EDIT_LAMBDA_EXPRESSION, "()", "method"));
        }

        [Fact]
        public void MethodToAsyncMethod_WithoutActiveStatement()
        {
            string src1 = @"
class C
{
    static Task<int> F()
    {
        Console.WriteLine(1);
        return Task.FromResult(1);
    }
}
";
            string src2 = @"
class C
{
    static async Task<int> F()
    {
        Console.WriteLine(1);
        return await Task.FromResult(1);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        #endregion

        #region Misplaced AS 

        [Fact]
        public void MisplacedActiveStatement1()
        {
            string src1 = @"
<AS:1>class C</AS:1>
{
    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
        <AS:2>return a;</AS:2> 
    }
}";
            string src2 = @"
class C
{
    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
        <AS:2>return a;</AS:2> 
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        #endregion

        #region Unmodified Documents

        [Fact]
        public void UnmodifiedDocument1()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        catch (IOException e) if (e == null)
        {
            Foo<AS:1>(</AS:1>);
        }
    }

    static void Foo()
    {
        Console.WriteLine(<AS:0>1</AS:0>);
    }
}";
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException e) when (e == null)
        {
            <AS:1>Foo();</AS:1>
        }</ER:1.0>
    }

    static void Foo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";

            var active = GetActiveStatements(src1, src2);
            Extensions.VerifyUnchangedDocument(src2, active);
        }

        [Fact]
        public void UnmodifiedDocument_BadSpans1()
        {
            string src1 = @"
class C
{
    <AS:2>const int a = 1;</AS:2>
 
    static void Main(string[] args)
    {
        Foo();
    }
<AS:1>
    static</AS:1> void Foo()
    {
        <AS:3>Console.WriteLine(1);</AS:3>
    }
}

<AS:0></AS:0>
";
            string src2 = @"
class C
{
    const int a = 1;

    static void Main(string[] args)
    {
       Foo();
    }

    static void Foo()
    <AS:1>{</AS:1>
        <AS:3>Console.WriteLine(1);</AS:3>
    }
}";

            var active = GetActiveStatements(src1, src2);
            Extensions.VerifyUnchangedDocument(src2, active);
        }

        #endregion

        #region Misc

        [Fact]
        public void Delete_All_SourceText()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}";
            string src2 = @"";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyRudeDiagnostics(
                Diagnostic(RudeEditKind.Delete, null, "class"));
        }

        #endregion
    }
}