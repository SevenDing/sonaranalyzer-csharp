﻿Imports System

Namespace Tests.Diagnostics
    Public Class A
        Public Const A As Integer = 5 ' Noncompliant
        Private Const B As Integer = 5
        Public C As Integer = 5
    End Class

    Friend Class B
        Public Const A As Integer = 5
        Private Const B As Integer = 5
        Public C As Integer = 5
    End Class
End Namespace