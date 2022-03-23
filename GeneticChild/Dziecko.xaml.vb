
' wejście: policzenie prawdopodobieństwa cech wedle osoby(iTyp=1) oraz osoby(iTyp=2)
' (założenie: mają one dobrze policzone prawdopodobieństwa)

Public NotInheritable Class Dziecko
    Inherits Page

    Dim mCechy As New List(Of JednaCecha)

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        If Not PoliczPrawdop() Then Return

        uiListItems.ItemsSource = mCechy

    End Sub
    Private Function PoliczPrawdop() As Boolean
        Dim oMe As JednaOsoba = App.glItems.ZnajdzOsobaTyp(1)
        Dim oShe As JednaOsoba = App.glItems.ZnajdzOsobaTyp(2)
        If oMe Is Nothing Or oShe Is Nothing Then Return False

        mCechy = ListaOsob.PoliczPrawdop(oMe, oShe)
        If mCechy.Count < 1 Then Return False
        Return True
    End Function

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.GoBack()
    End Sub

    Private Async Sub uiCopyData_Click(sender As Object, e As RoutedEventArgs)

        ' wyliczenie maxLen cechy
        Dim iMaxLen As Integer = 0
        For Each oCecha As JednaCecha In mCechy
            iMaxLen = Math.Max(iMaxLen, oCecha.sName.Length)
        Next
        iMaxLen += 1

        Dim oMsg As Email.EmailMessage = New Windows.ApplicationModel.Email.EmailMessage()
        oMsg.Subject = GetLangString("msgOsobaEmailSubject")

        Dim sTxt As String = GetLangString("msgDzieckoEmailBodyHdr") & vbCrLf & vbCrLf &
            "Data: " & Date.Now & vbCrLf & vbCrLf

        For Each oCecha As JednaCecha In mCechy
            sTxt = sTxt & oCecha.sName.PadRight(iMaxLen) & oCecha.dGenotyp & vbCrLf
        Next

        oMsg.Body = sTxt

        Await Email.EmailManager.ShowComposeNewEmailAsync(oMsg)


    End Sub
End Class


Public Class KonwersjaPrawdop
    Implements IValueConverter

    ' Define the Convert method to change a DateTime object to
    ' a month string.
    Public Function Convert(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.Convert

        ' value is the data from the source object.

        Dim dTmp As Double = CType(value, Double)
        If dTmp < 0 Then Return "???"
        Return (dTmp * 100).ToString("#0") & " %"

    End Function

    ' ConvertBack is not implemented for a OneWay binding.
    Public Function ConvertBack(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.ConvertBack

        Throw New NotImplementedException

    End Function
End Class
