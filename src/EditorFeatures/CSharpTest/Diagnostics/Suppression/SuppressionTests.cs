﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Suppression
{
    public abstract class CSharpSuppressionTests : AbstractSuppressionDiagnosticTest
    {
        protected override ParseOptions GetScriptOptions()
        {
            return Options.Script;
        }

        protected override TestWorkspace CreateWorkspaceFromFile(string definition, ParseOptions parseOptions, CompilationOptions compilationOptions)
        {
            return CSharpWorkspaceFactory.CreateWorkspaceFromFile(definition, (CSharpParseOptions)parseOptions, (CSharpCompilationOptions)compilationOptions);
        }

        protected override string GetLanguage()
        {
            return LanguageNames.CSharp;
        }

        #region "Pragma disable tests"

        public abstract class CSharpPragmaWarningDisableSuppressionTests : CSharpSuppressionTests
        {
            protected sealed override int CodeActionIndex
            {
                get { return 0; }
            }

            public class CompilerDiagnosticSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
            {
                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return Tuple.Create<DiagnosticAnalyzer, ISuppressionFixProvider>(null, new CSharpSuppressionCodeFixProvider());
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestPragmaWarningDirective()
                {
                    Test(
        @"
class Class
{
    void Method()
    {
        [|int x = 0;|]
    }
}",
        @"
class Class
{
    void Method()
    {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        int x = 0;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
    }
}");
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestMultilineStatementPragmaWarningDirective()
                {
                    Test(
        @"
class Class
{
    void Method()
    {
        [|int x = 0
              + 1;|]
    }
}",
        @"
class Class
{
    void Method()
    {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        int x = 0
#pragma warning restore CS0219 // Variable is assigned but its value is never used
              + 1;
    }
}");
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestPragmaWarningDirectiveWithExistingTrivia()
                {
                    Test(
        @"
class Class
{
    void Method()
    {
        // Start comment previous line
        /* Start comment same line */ [|int x = 0;|] // End comment same line
        /* End comment next line */
    }
}",
        @"
class Class
{
    void Method()
    {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                              // Start comment previous line
                              /* Start comment same line */
        int x = 0; // End comment same line
#pragma warning restore CS0219 // Variable is assigned but its value is never used
                              /* End comment next line */
    }
}");
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestMultipleInstancesOfPragmaWarningDirective()
                {
                    Test(
        @"
class Class
{
    void Method()
    {
        [|int x = 0, y = 0;|]
    }
}",
        @"
class Class
{
    void Method()
    {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        int x = 0, y = 0;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
    }
}");
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestErrorAndWarningScenario()
                {
                    Test(
        @"
class Class
{
    void Method()
    {
        return 0;
        [|int x = ""0"";|]
    }
}",
        @"
class Class
{
    void Method()
    {
        return 0;
#pragma warning disable CS0162 // Unreachable code detected
        int x = ""0"";
#pragma warning restore CS0162 // Unreachable code detected
    }
}");
                }

                [WorkItem(956453)]
                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestWholeFilePragmaWarningDirective()
                {
                    Test(
        @"class Class { void Method() { [|int x = 0;|] } }",
        @"#pragma warning disable CS0219 // Variable is assigned but its value is never used
class Class { void Method() { int x = 0; } }
#pragma warning restore CS0219 // Variable is assigned but its value is never used");
                }

                [WorkItem(970129)]
                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionAroundSingleToken()
                {
                    Test(
        @"
using System;
[Obsolete]
class Session { }
class Program
{
    static void Main()
    {
      [|Session|]
    }
}",
        @"
using System;
[Obsolete]
class Session { }
class Program
{
    static void Main()
    {
#pragma warning disable CS0612 // Type or member is obsolete
        Session
#pragma warning restore CS0612 // Type or member is obsolete
    }
}");
                }

                [WorkItem(1066576)]
                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestPragmaWarningDirectiveAroundTrivia1()
                {
                    Test(
        @"
class Class
{
    void Method()
    {

// Comment
// Comment
[|#pragma abcde|]

    }    // Comment   



}",
        @"
class Class
{
    void Method()
    {

#pragma warning disable CS1633 // Unrecognized #pragma directive
                              // Comment
                              // Comment
#pragma abcde

    }    // Comment   
#pragma warning restore CS1633 // Unrecognized #pragma directive



}");
                }

                [WorkItem(1066576)]
                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestPragmaWarningDirectiveAroundTrivia2()
                {
                    Test(
        @"[|#pragma abcde|]",
        @"#pragma warning disable CS1633 // Unrecognized #pragma directive
#pragma abcde
#pragma warning restore CS1633 // Unrecognized #pragma directive");
                }

                [WorkItem(1066576)]
                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestPragmaWarningDirectiveAroundTrivia3()
                {
                    Test(
        @"  [|#pragma abcde|]  ",
        @"#pragma warning disable CS1633 // Unrecognized #pragma directive
#pragma abcde  
#pragma warning restore CS1633 // Unrecognized #pragma directive");
                }

                [WorkItem(1066576)]
                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestPragmaWarningDirectiveAroundTrivia4()
                {
                    Test(
        @"

[|#pragma abc|]
class C { }

",
        @"

#pragma warning disable CS1633 // Unrecognized #pragma directive
#pragma abc
class C { }
#pragma warning restore CS1633 // Unrecognized #pragma directive

");
                }

                [WorkItem(1066576)]
                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestPragmaWarningDirectiveAroundTrivia5()
                {
                    Test(
        @"class C1 { }
[|#pragma abc|]
class C2 { }
class C3 { }",
        @"class C1 { }
#pragma warning disable CS1633 // Unrecognized #pragma directive
#pragma abc
class C2 { }
#pragma warning restore CS1633 // Unrecognized #pragma directive
class C3 { }");
                }

                [WorkItem(1066576)]
                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestPragmaWarningDirectiveAroundTrivia6()
                {
                    Test(
        @"class C1 { }
class C2 { } /// <summary><see [|cref=""abc""|]/></summary>
class C3 { } // comment
  // comment
// comment",
        @"class C1 { }
class C2 { }
#pragma warning disable CS1574
/// <summary><see cref=""abc""/></summary>
class C3 { } // comment
#pragma warning enable CS1574
// comment
// comment", CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose));
                }
            }

            public class UserHiddenDiagnosticSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
            {
                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                        new CSharpSimplifyTypeNamesDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestHiddenDiagnosticCannotBeSuppressed()
                {
                    TestMissing(
        @"
using System;

class Class
{
int Method()
{
    [|System.Int32 x = 0;|]
    return x;
}
}");
                }
            }

            public class UserInfoDiagnosticSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
            {
                private class UserDiagnosticAnalyzer : DiagnosticAnalyzer
                {
                    private DiagnosticDescriptor _descriptor =
                        new DiagnosticDescriptor("InfoDiagnostic", "InfoDiagnostic Title", "InfoDiagnostic", "InfoDiagnostic", DiagnosticSeverity.Info, isEnabledByDefault: true);

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(_descriptor);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
                    }

                    public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                    {
                        var classDecl = (ClassDeclarationSyntax)context.Node;
                        context.ReportDiagnostic(Diagnostic.Create(_descriptor, classDecl.Identifier.GetLocation()));
                    }
                }

                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                        new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestInfoDiagnosticSuppressed()
                {
                    Test(
            @"
using System;

[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}",
            @"
using System;

#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
class Class
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
{
    int Method()
    {
        int x = 0;
    }
}");
                }
            }

            public class UserErrorDiagnosticSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
            {
                private class UserDiagnosticAnalyzer : DiagnosticAnalyzer
                {
                    private DiagnosticDescriptor _descriptor =
                        new DiagnosticDescriptor("ErrorDiagnostic", "ErrorDiagnostic", "ErrorDiagnostic", "ErrorDiagnostic", DiagnosticSeverity.Error, isEnabledByDefault: true);

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(_descriptor);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
                    }

                    public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                    {
                        var classDecl = (ClassDeclarationSyntax)context.Node;
                        context.ReportDiagnostic(Diagnostic.Create(_descriptor, classDecl.Identifier.GetLocation()));
                    }
                }

                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                        new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestErrorDiagnosticCannotBeSuppressed()
                {
                    TestMissing(
            @"
using System;

[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}");
                }
            }

            public class DiagnosticWithBadIdSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
            {
                private class UserDiagnosticAnalyzer : DiagnosticAnalyzer
                {
                    private DiagnosticDescriptor _descriptor =
                        new DiagnosticDescriptor("@~DiagnosticWithBadId", "DiagnosticWithBadId", "DiagnosticWithBadId", "DiagnosticWithBadId", DiagnosticSeverity.Info, isEnabledByDefault: true);

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(_descriptor);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
                    }

                    public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                    {
                        var classDecl = (ClassDeclarationSyntax)context.Node;
                        context.ReportDiagnostic(Diagnostic.Create(_descriptor, classDecl.Identifier.GetLocation()));
                    }
                }

                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                        new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestDiagnosticWithBadIdSuppressed()
                {
                    Test(
            @"
using System;

[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}",
            @"
using System;

#pragma warning disable @~DiagnosticWithBadId // DiagnosticWithBadId
class Class
#pragma warning restore @~DiagnosticWithBadId // DiagnosticWithBadId
{
    int Method()
    {
        int x = 0;
    }
}");

                    // Verify that the original suppression doesn't really work and that the diagnostic can be suppressed again.
                    Test(
            @"
using System;

#pragma warning disable @~DiagnosticWithBadId // DiagnosticWithBadId
[|class Class|]
#pragma warning restore @~DiagnosticWithBadId // DiagnosticWithBadId
{
    int Method()
    {
        int x = 0;
    }
}",
            @"
using System;

#pragma warning disable @~DiagnosticWithBadId // DiagnosticWithBadId
#pragma warning disable @~DiagnosticWithBadId // DiagnosticWithBadId
class Class
#pragma warning restore @~DiagnosticWithBadId // DiagnosticWithBadId
#pragma warning restore @~DiagnosticWithBadId // DiagnosticWithBadId
{
    int Method()
    {
        int x = 0;
    }
}");
                }
            }
        }

        #endregion

        #region "SuppressMessageAttribute tests"

        public abstract class CSharpGlobalSuppressMessageSuppressionTests : CSharpSuppressionTests
        {
            protected sealed override int CodeActionIndex
            {
                get { return 2; }
            }

            public class CompilerDiagnosticSuppressionTests : CSharpGlobalSuppressMessageSuppressionTests
            {
                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return Tuple.Create<DiagnosticAnalyzer, ISuppressionFixProvider>(null, new CSharpSuppressionCodeFixProvider());
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestCompilerDiagnosticsCannotBeSuppressed()
                {
                    // Another test verifies we have a pragma warning action for this source, this verifies there are no other suppression actions.
                    TestActionCount(
        @"
class Class
{
    void Method()
    {
        [|int x = 0;|]
    }
}", 1);
                }
            }

            public class UserHiddenDiagnosticSuppressionTests : CSharpGlobalSuppressMessageSuppressionTests
            {
                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                        new CSharpSimplifyTypeNamesDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestHiddenDiagnosticsCannotBeSuppressed()
                {
                    TestMissing(
        @"
using System;
class Class
{
    void Method()
    {
        [|System.Int32 x = 0;|]
    }
}");
                }
            }

            public class UserInfoDiagnosticSuppressionTests : CSharpGlobalSuppressMessageSuppressionTests
            {
                private class UserDiagnosticAnalyzer : DiagnosticAnalyzer
                {
                    private DiagnosticDescriptor _descriptor =
                        new DiagnosticDescriptor("InfoDiagnostic", "InfoDiagnostic", "InfoDiagnostic", "InfoDiagnostic", DiagnosticSeverity.Info, isEnabledByDefault: true);

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(_descriptor);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration, SyntaxKind.NamespaceDeclaration, SyntaxKind.MethodDeclaration, SyntaxKind.PropertyDeclaration, SyntaxKind.FieldDeclaration, SyntaxKind.EventDeclaration);
                    }

                    public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                    {
                        switch (context.Node.Kind())
                        {
                            case SyntaxKind.ClassDeclaration:
                                var classDecl = (ClassDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, classDecl.Identifier.GetLocation()));
                                break;

                            case SyntaxKind.NamespaceDeclaration:
                                var ns = (NamespaceDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, ns.Name.GetLocation()));
                                break;

                            case SyntaxKind.MethodDeclaration:
                                var method = (MethodDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, method.Identifier.GetLocation()));
                                break;

                            case SyntaxKind.PropertyDeclaration:
                                var property = (PropertyDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, property.Identifier.GetLocation()));
                                break;

                            case SyntaxKind.FieldDeclaration:
                                var field = (FieldDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, field.Declaration.Variables.First().Identifier.GetLocation()));
                                break;

                            case SyntaxKind.EventDeclaration:
                                var e = (EventDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, e.Identifier.GetLocation()));
                                break;
                        }
                    }
                }

                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                        new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnSimpleType()
                {
                    Test(
            @"
using System;

[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}",
            @"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""~T:Class"")]

", isAddedDocument: true);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    TestMissing(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""~T:Class"")]

[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}");
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnNamespace()
                {
                    Test(
            @"
using System;

[|namespace N|]
{
    class Class
    {
        int Method()
        {
            int x = 0;
        }
    }
}",
            @"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""namespace"", Target = ""~N:N"")]

", index: 1, isAddedDocument: true);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    TestMissing(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""namespace"", Target = ""~N:N"")]

[|namespace N|]
{
    class Class
    {
        int Method()
        {
            int x = 0;
        }
    }
}");
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnTypeInsideNamespace()
                {
                    Test(
            @"
using System;

namespace N1
{
    namespace N2
    {
        [|class Class|]
        {
            int Method()
            {
                int x = 0;
            }
        }
    }
}",
            @"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""~T:N1.N2.Class"")]

", isAddedDocument: true);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    TestMissing(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""~T:N1.N2.Class"")]

namespace N1
{
    namespace N2
    {
        [|class Class|]
        {
            int Method()
            {
                int x = 0;
            }
        }
    }
}");
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnNestedType()
                {
                    Test(
            @"
using System;

namespace N
{
    class Generic<T>
    {
        [|class Class|]
        {
            int Method()
            {
                int x = 0;
            }
        }
    }
}",
            @"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""~T:N.Generic`1.Class"")]

", isAddedDocument: true);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    TestMissing(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""~T:N.Generic`1.Class"")]

namespace N
{
    class Generic<T>
    {
        [|class Class|]
        {
            int Method()
            {
                int x = 0;
            }
        }
    }
}");
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnMethod()
                {
                    Test(
            @"
using System;

namespace N
{
    class Generic<T>
    {
        class Class
        {
            [|int Method()
            {
                int x = 0;
            }|]
        }
    }
}",
            @"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method~System.Int32"")]

", isAddedDocument: true);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    TestMissing(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method~System.Int32"")]

namespace N
{
    class Generic<T>
    {
        class Class
        {
            [|int Method()|]
            {
                int x = 0;
            }
        }
    }
}");
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnOverloadedMethod()
                {
                    Test(
            @"
using System;

namespace N
{
    class Generic<T>
    {
        class Class
        {
            [|int Method(int y, ref char z)
            {
                int x = 0;
            }|]

            int Method()
            {
                int x = 0;
            }
        }
    }
}",
            @"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method(System.Int32,System.Char@)~System.Int32"")]

", isAddedDocument: true);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    TestMissing(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method(System.Int32,System.Char@)~System.Int32"")]

namespace N
{
    class Generic<T>
    {
        class Class
        {
            [|int Method(int y, ref char z)|]
            {
                int x = 0;
            }

            int Method()
            {
                int x = 0;
            }
        }
    }
}");

                    Test(
        @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method(System.Int32,System.Char@)~System.Int32"")]

namespace N
{
    class Generic<T>
    {
        class Class
        {
            [|int Method(int y, ref char z)
            {
                int x = 0;
            }

            int Method()
            {
                int x = 0;
            }|]
        }
    }
}",
            @"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method~System.Int32"")]

", isAddedDocument: true);
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnGenericMethod()
                {
                    Test(
            @"
using System;

namespace N
{
    class Generic<T>
    {
        class Class
        {
            [|int Method<U>(U u)
            {
                int x = 0;
            }|]
        }
    }
}",
            @"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method``1(``0)~System.Int32"")]

", isAddedDocument: true);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    TestMissing(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~M:N.Generic`1.Class.Method``1(``0)~System.Int32"")]

namespace N
{
    class Generic<T>
    {
        class Class
        {
            [|int Method<U>(U u)|]
            {
                int x = 0;
            }
        }
    }
}");
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnProperty()
                {
                    Test(
            @"
using System;

namespace N
{
    class Generic
    {
        class Class
        {
            [|int Property|]
            {
                get { int x = 0; }
            }
        }
    }
}",
            @"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~P:N.Generic.Class.Property"")]

", isAddedDocument: true);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    TestMissing(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~P:N.Generic.Class.Property"")]

namespace N
{
    class Generic
    {
        class Class
        {
            [|int Property|]
            {
                get { int x = 0; }
            }
        }
    }
}");
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnField()
                {
                    Test(
            @"
using System;

class Class
{
    [|int field = 0;|]
}",
            @"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~F:Class.field"")]

", isAddedDocument: true);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    TestMissing(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~F:Class.field"")]

class Class
{
    [|int field = 0;|]
}");
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnField2()
                {
                    Test(
            @"
using System;

class Class
{
    int [|field = 0|], field2 = 1;
}",
            @"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~F:Class.field"")]

", isAddedDocument: true);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    TestMissing(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~F:Class.field"")]

class Class
{
    int [|field|] = 0, field2 = 1;
}");
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnEvent()
                {
                    Test(
            @"
using System;

public class SampleEventArgs
{
    public SampleEventArgs(string s) { Text = s; }
    public String Text {get; private set;} // readonly
        }

class Class
{
    // Declare the delegate (if using non-generic pattern). 
    public delegate void SampleEventHandler(object sender, SampleEventArgs e);

    // Declare the event. 
    [|public event SampleEventHandler SampleEvent
    {
        add { }
        remove { }
    }|]
}",
            @"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~E:Class.SampleEvent"")]

", isAddedDocument: true);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    TestMissing(
            @"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""member"", Target = ""~E:Class.SampleEvent"")]

public class SampleEventArgs
{
    public SampleEventArgs(string s) { Text = s; }
    public String Text {get; private set;} // readonly
}

class Class
{
    // Declare the delegate (if using non-generic pattern). 
    public delegate void SampleEventHandler(object sender, SampleEventArgs e);

    // Declare the event. 
    [|public event SampleEventHandler SampleEvent|]
    {
        add { }
        remove { }
    }
}");
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionWithExistingGlobalSuppressionsDocument()
                {
                    var initialMarkup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
using System;

class Class { }

[|class Class2|] { }
]]>
        </Document>
        <Document FilePath=""GlobalSuppressions.cs""><![CDATA[
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""Class"")]
]]>
        </Document>
    </Project>
</Workspace>";
                    var expectedText =
                        @"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""Class"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""~T:Class2"")]

";

                    Test(initialMarkup, expectedText, isLine: false);
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionWithExistingGlobalSuppressionsDocument2()
                {
                    // Own custom file named GlobalSuppressions.cs
                    var initialMarkup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
using System;

class Class { }

[|class Class2|] { }
]]>
        </Document>
        <Document FilePath=""GlobalSuppressions.cs""><![CDATA[
// My own file named GlobalSuppressions.cs.
using System;
class Class { }
]]>
        </Document>
    </Project>
</Workspace>";
                    var expectedText =
                        @"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""~T:Class2"")]

";

                    Test(initialMarkup, expectedText, isLine: false, isAddedDocument: true);
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionWithExistingGlobalSuppressionsDocument3()
                {
                    // Own custom file named GlobalSuppressions.cs + existing GlobalSuppressions2.cs with global suppressions
                    var initialMarkup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
using System;

class Class { }

[|class Class2|] { }
]]>
        </Document>
        <Document FilePath=""GlobalSuppressions.cs""><![CDATA[
// My own file named GlobalSuppressions.cs.
using System;
class Class { }
]]>
        </Document>
         <Document FilePath=""GlobalSuppressions2.cs""><![CDATA[
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""Class"")]
]]>
        </Document>
    </Project>
</Workspace>";
                    var expectedText =
                        @"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""Class"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"", Scope = ""type"", Target = ""~T:Class2"")]

";

                    Test(initialMarkup, expectedText, isLine: false, isAddedDocument: false);
                }
            }
        }

        public abstract class CSharpLocalSuppressMessageSuppressionTests : CSharpSuppressionTests
        {
            protected sealed override int CodeActionIndex
            {
                get { return 1; }
            }

            public class UserInfoDiagnosticSuppressionTests : CSharpLocalSuppressMessageSuppressionTests
            {
                private class UserDiagnosticAnalyzer : DiagnosticAnalyzer
                {
                    private DiagnosticDescriptor _descriptor =
                        new DiagnosticDescriptor("InfoDiagnostic", "InfoDiagnostic", "InfoDiagnostic", "InfoDiagnostic", DiagnosticSeverity.Info, isEnabledByDefault: true);

                    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                    {
                        get
                        {
                            return ImmutableArray.Create(_descriptor);
                        }
                    }

                    public override void Initialize(AnalysisContext context)
                    {
                        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration, SyntaxKind.NamespaceDeclaration, SyntaxKind.MethodDeclaration);
                    }

                    public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                    {
                        switch (context.Node.Kind())
                        {
                            case SyntaxKind.ClassDeclaration:
                                var classDecl = (ClassDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, classDecl.Identifier.GetLocation()));
                                break;

                            case SyntaxKind.NamespaceDeclaration:
                                var ns = (NamespaceDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, ns.Name.GetLocation()));
                                break;

                            case SyntaxKind.MethodDeclaration:
                                var method = (MethodDeclarationSyntax)context.Node;
                                context.ReportDiagnostic(Diagnostic.Create(_descriptor, method.Identifier.GetLocation()));
                                break;
                        }
                    }
                }

                internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
                {
                    return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                        new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnSimpleType()
                {
                    var initial = @"
using System;

// Some trivia
/* More Trivia */ [|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}";
                    var expected = @"
using System;

// Some trivia
/* More Trivia */
[System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"")]
class Class
{
    int Method()
    {
        int x = 0;
    }
}";
                    Test(initial, expected);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    expected = expected.Replace("class Class", "[|class Class|]");
                    TestMissing(expected);
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnSimpleType2()
                {
                    // Type already has attributes.
                    var initial = @"
using System;

// Some trivia
/* More Trivia */
[System.Diagnostics.CodeAnalysis.SuppressMessage(""SomeOtherDiagnostic"", ""SomeOtherDiagnostic:Title"", Justification = ""<Pending>"")]
[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}";
                    var expected = @"
using System;

// Some trivia
/* More Trivia */
[System.Diagnostics.CodeAnalysis.SuppressMessage(""SomeOtherDiagnostic"", ""SomeOtherDiagnostic:Title"", Justification = ""<Pending>"")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"")]
class Class
{
    int Method()
    {
        int x = 0;
    }
}";
                    Test(initial, expected);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    expected = expected.Replace("class Class", "[|class Class|]");
                    TestMissing(expected);
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnSimpleType3()
                {
                    // Type already has attributes with trailing trivia.
                    var initial = @"
using System;

// Some trivia
/* More Trivia */
[System.Diagnostics.CodeAnalysis.SuppressMessage(""SomeOtherDiagnostic"", ""SomeOtherDiagnostic:Title"", Justification = ""<Pending>"")]
/* Some More Trivia */
[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}";
                    var expected = @"
using System;

// Some trivia
/* More Trivia */
[System.Diagnostics.CodeAnalysis.SuppressMessage(""SomeOtherDiagnostic"", ""SomeOtherDiagnostic:Title"", Justification = ""<Pending>"")]
[System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"")]
/* Some More Trivia */
class Class
{
    int Method()
    {
        int x = 0;
    }
}";
                    Test(initial, expected);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    expected = expected.Replace("class Class", "[|class Class|]");
                    TestMissing(expected);
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnTypeInsideNamespace()
                {
                    var initial = @"
using System;

namespace N1
{
    namespace N2
    {
        [|class Class|]
        {
            int Method()
            {
                int x = 0;
            }
        }
    }
}";
                    var expected = @"
using System;

namespace N1
{
    namespace N2
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"")]
        class Class
        {
            int Method()
            {
                int x = 0;
            }
        }
    }
}";
                    Test(initial, expected);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    expected = expected.Replace("class Class", "[|class Class|]");
                    TestMissing(expected);
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnNestedType()
                {
                    var initial = @"
using System;

namespace N
{
    class Generic<T>
    {
        [|class Class|]
        {
            int Method()
            {
                int x = 0;
            }
        }
    }
}";
                    var expected = @"
using System;

namespace N
{
    class Generic<T>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"")]
        class Class
        {
            int Method()
            {
                int x = 0;
            }
        }
    }
}";
                    Test(initial, expected);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    expected = expected.Replace("class Class", "[|class Class|]");
                    TestMissing(expected);
                }

                [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                public void TestSuppressionOnMethod()
                {
                    var initial = @"
using System;

namespace N
{
    class Generic<T>
    {
        class Class
        {
            [|int Method()|]
            {
                int x = 0;
            }
        }
    }
}";
                    var expected = @"
using System;

namespace N
{
    class Generic<T>
    {
        class Class
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""<Pending>"")]
            int Method()
            {
                int x = 0;
            }
        }
    }
}";
                    Test(initial, expected);

                    // Also verify that the added attribute does indeed suppress the diagnostic.
                    expected = expected.Replace("int Method()", "[|int Method()|]");
                    TestMissing(expected);
                }
            }
        }

        #endregion

        #region NoLocation Diagnostics tests

        public class CSharpDiagnosticWithoutLocationSuppressionTests : CSharpSuppressionTests
        {
            private class UserDiagnosticAnalyzer : DiagnosticAnalyzer
            {
                private DiagnosticDescriptor _descriptor =
                    new DiagnosticDescriptor("NoLocationDiagnostic", "NoLocationDiagnostic", "NoLocationDiagnostic", "NoLocationDiagnostic", DiagnosticSeverity.Info, isEnabledByDefault: true);

                public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                {
                    get
                    {
                        return ImmutableArray.Create(_descriptor);
                    }
                }

                public override void Initialize(AnalysisContext context)
                {
                    context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
                }

                public void AnalyzeNode(SyntaxNodeAnalysisContext context)
                {
                    context.ReportDiagnostic(Diagnostic.Create(_descriptor, Location.None));
                }
            }

            internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
            {
                return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                    new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
            }

            protected override int CodeActionIndex
            {
                get
                {
                    return 0;
                }
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            [WorkItem(1073825)]
            public void TestDiagnosticWithoutLocationCannotBeSuppressed()
            {
                TestMissing(
        @"
using System;

[|class Class|]
{
    int Method()
    {
        int x = 0;
    }
}");
            }
        }
        #endregion
    }
}