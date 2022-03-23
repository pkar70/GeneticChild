' teksty: GetLang("cecha00rec") / GetLang("cecha00dom")

Public Class JednaCecha
    'Public Property iId As Integer
    Public Property sName As String
    Public Property dGenotyp As Double
    'Public Property sLink As String ' do tekstu / artykułu?
    'Public Property sPicLink As String ' do obrazka 
End Class

Public Class JednaCechaOsoby
    Public Property iIdCecha As Integer
    Public Property iFenotyp As Integer = -1 ' czy widać cechę, 0: nie, 1: tak, -1: nie wiadomo
    Public Property dGenotyp As Double = -1 ' wyliczane hierarchicznie prawdopodobieństwo, -1: nie ma danych
End Class

Public Class JednaOsoba
    Public Property iId As Integer
    Public Property sName As String
    Public Property iTyp As Integer = -1    ' 1: me, 2: spouse, -1: inny
    Public Property sNote As String = ""    ' dowolne coś, nie interpretowane
    Public Property iIdMother As Integer = -1
    Public Property iIdFather As Integer = -1
    Public Property dUrodziny As DateTimeOffset ' do odróżniania gdy jest kilka tych samych name (np. Adam Bielański (1970) itp.)
    Public Property lCechy As New List(Of JednaCechaOsoby)
End Class

Public Class ListaOsob
    Private mItems As New ObservableCollection(Of JednaOsoba)
    Private bModified As Boolean = False
    Private Const FILE_NAME As String = "osoby.json"
    Private msFileName As String = ""

    Public Sub New(sFolderPath As String)
        msFileName = IO.Path.Combine(sFolderPath, FILE_NAME)
    End Sub

    Public Function Load(Optional bForce As Boolean = False) As Boolean
        DumpCurrMethod()

        If IsLoaded() AndAlso Not bForce Then Return True

        bModified = False

        If Not IO.File.Exists(msFileName) Then
            mItems = New ObservableCollection(Of JednaOsoba)
            Return False
        End If

        Dim sTxt As String = IO.File.ReadAllText(msFileName)
        If sTxt Is Nothing OrElse sTxt.Length < 5 Then
            mItems = New ObservableCollection(Of JednaOsoba)
            Return False
        End If

        mItems = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, mItems.GetType)

        Return True

    End Function

    Public Function Save(bForce As Boolean) As Boolean
        If Not bModified AndAlso Not bForce Then Return False
        If mItems.Count < 1 Then Return False

        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(mItems, Newtonsoft.Json.Formatting.Indented)
        IO.File.WriteAllText(msFileName, sTxt)

        bModified = False
        Return True

    End Function

    Private Function GetMaxId() As Integer
        Dim iMaxId As Integer = -1
        For Each oItem As JednaOsoba In mItems
            iMaxId = Math.Max(iMaxId, oItem.iId)
        Next

        Return iMaxId
    End Function
    Public Function Add(oNew As JednaOsoba) As Integer
        If oNew Is Nothing Then Return -1

        If mItems Is Nothing Then
            mItems = New ObservableCollection(Of JednaOsoba)
        End If

        bModified = True

        ' identyfikator bardzo proszę dodać, kolejny możliwy
        Dim iMaxId As Integer = GetMaxId()
        oNew.iId = iMaxId + 1


        mItems.Add(oNew)

        Return oNew.iId
    End Function

    Public Function Remove(oDel As JednaOsoba) As Boolean
        Try
            mItems.Remove(oDel)
            Return True
        Catch ex As Exception
            Return False
        End Try

    End Function

    Public Sub Clear()
        bModified = True
        mItems.Clear()
    End Sub

    Public Function IsLoaded() As Boolean
        If mItems Is Nothing Then Return False
        If mItems.Count < 1 Then Return False
        Return True
    End Function

    Public Function GetList() As ObservableCollection(Of JednaOsoba)
        Return mItems
    End Function

    Public Function Count() As Integer
        If mItems Is Nothing Then Return -1
        Return mItems.Count
    End Function

    Public Sub MakeDirty()
        bModified = True
    End Sub

    Public Function CzyJestOsobaTyp(iTyp As Integer) As Boolean
        For Each oItem As JednaOsoba In mItems
            If oItem.iTyp = iTyp Then Return True
        Next
        Return False
    End Function

    Public Function ZnajdzOsobaTyp(iTyp As Integer) As JednaOsoba
        For Each oItem As JednaOsoba In mItems
            If oItem.iTyp = iTyp Then Return oItem
        Next
        Return Nothing
    End Function

    Public Function ZnajdzOsobaId(iId As Integer) As JednaOsoba
        For Each oItem As JednaOsoba In mItems
            If oItem.iId = iId Then Return oItem
        Next
        Return Nothing
    End Function

    'Public Function DumpMyData() As String
    '    Dim oOsoba As JednaOsoba = ZnajdzOsobaTyp(1)
    '    Return DumpMyData(oOsoba)
    'End Function

    Public Function DumpData(oOsoba As JednaOsoba) As String
        ' przy zmianie tu: zmienić w Osoba:ParseCopiedData
        Dim sRet As String = ""

        If oOsoba Is Nothing Then Return sRet

        sRet = oOsoba.sName & vbCrLf & vbCrLf

        For Each oCecha As JednaCechaOsoby In oOsoba.lCechy
            If App.GetCechaName(oCecha.iIdCecha).Length < 4 Then Continue For  ' wysyłamy tylko te cechy, które znamy
            sRet = sRet & oCecha.iIdCecha & vbTab & oCecha.iFenotyp & vbTab & oCecha.dGenotyp & vbCrLf
        Next

        Return sRet
    End Function



    Public Sub PrzeliczCechyWDrzewku(iRootId As Integer)
        DumpCurrMethod("(" & iRootId)
        ' przeliczenie cech w drzewie, 'odtąd w dół'
        ' wszystkie cechy na nowo liczymy, bo zmiana w iFenotyp, więc ewentualnie też iGenotyp

        Dim oOsoba As JednaOsoba = ZnajdzOsobaId(iRootId)
        If oOsoba Is Nothing Then
            DumpMessage("Nie ma takiej osoby?")
            Return
        End If

        DumpMessage("osoba: " & oOsoba.sName)

        Dim oMama As JednaOsoba = ZnajdzOsobaId(oOsoba.iIdMother)
        Dim oTata As JednaOsoba = ZnajdzOsobaId(oOsoba.iIdFather)

        ' dla każdej cechy, policz cechę dziecka wedle rodziców
        For Each oCecha As JednaCechaOsoby In oOsoba.lCechy
            DumpMessage("Cecha: " & oCecha.iIdCecha & " = " & App.GetCechaName(oCecha.iIdCecha), 1)
            If oCecha.iFenotyp = 0 Then
                DumpMessage("- fenotyp recesywny, więc genotyp 0", 1)
                ' na pewno tak jest, przy recesywnym obie są recesywne
                oCecha.dGenotyp = 0
            Else
                ' teraz w zależności od rodziców
                If oMama IsNot Nothing And oTata IsNot Nothing Then
                    oCecha.dGenotyp = PoliczCecheDziecka(oCecha.iIdCecha, oTata, oMama)
                    DumpMessage("- wedle rodziców, genotyp " & oCecha.dGenotyp, 1)
                Else
                    DumpMessage("- brak przynajmniej jednego rodzica, wyliczam genotyp z fenotypu", 1)
                    oCecha.dGenotyp = CechaGenotypFromFenotyp(oCecha.iFenotyp, -1)
                End If
            End If
        Next

        ' znajdź potomka - zakładamy max jednego, i w nim przelicz rekurencyjnie
        ' tylko jeden: bo drzewko jest bardzo cienkie, znaczy tylko w linii prostej (bez bocznych)
        For Each oChild As JednaOsoba In mItems
            If oChild.iIdFather = iRootId Or oChild.iIdMother = iRootId Then PrzeliczCechyWDrzewku(oChild.iId)
        Next

        MakeDirty()
    End Sub

    Private Shared Function CechaGenotypFromFenotyp(iFenotyp As Integer, dGenotyp As Double) As Double
        If dGenotyp > -1 Then Return dGenotyp

        If iFenotyp < 0 Then Return -1  ' skoro nie wiemy jak jest, to error

        ' przeliczenie z fenotypu na genotyp
        If iFenotyp = 0 Then Return 0   ' recesywna widoczna, czyli na pewno nie ma genetycznie

        ' pozostaje przypadek że widoczna dominująca
        Return 0.66 ' tzn. recesywne wystąpi z 1/3, tu jest 2/3
        ' (mamy bowiem możliwości: XX, Xx, xX , X jest 4 razy, a x tylko dwa razy)

    End Function

    Public Shared Function PoliczCecheDziecka(iCecha As Integer, oMe As JednaOsoba, oShe As JednaOsoba) As Double
        DumpCurrMethod("(" & iCecha & ", " & oMe.sName & ", " & oShe.sName)

        If oMe Is Nothing Then Return -1
        If oShe Is Nothing Then Return -1

        Dim oCechaMe As JednaCechaOsoby = Nothing
        Dim oCechaShe As JednaCechaOsoby = Nothing

        For Each oCecha As JednaCechaOsoby In oMe.lCechy
            If oCecha.iIdCecha = iCecha Then
                oCechaMe = oCecha
                Exit For
            End If
        Next
        If oCechaMe Is Nothing Then Return -1

        For Each oCecha As JednaCechaOsoby In oShe.lCechy
            If oCecha.iIdCecha = iCecha Then
                oCechaShe = oCecha
                Exit For
            End If
        Next
        If oCechaShe Is Nothing Then Return -1

        DumpMessage("Me:  " & oCechaMe.dGenotyp & vbTab & oCechaMe.iFenotyp, 1)
        DumpMessage("She: " & oCechaShe.dGenotyp & vbTab & oCechaShe.iFenotyp, 1)

        If oCechaMe.dGenotyp = -1 And oCechaMe.iFenotyp = -1 Then Return -1     ' brak danych o iCecha u mnie
        If oCechaShe.dGenotyp = -1 And oCechaShe.iFenotyp = -1 Then Return -1   ' brak danych o iCecha u niej

        If oCechaMe.iFenotyp = 0 And oCechaShe.iFenotyp = 0 Then Return 0   ' na pewno nie ma cechy (oba recesywne)

        ' teraz sprawdzamy wedle genotypu (wyliczanego 'kiedyś tam')
        Dim dPrawdopMe As Double = CechaGenotypFromFenotyp(oCechaMe.iFenotyp, oCechaMe.dGenotyp)
        Dim dPrawdopShe As Double = CechaGenotypFromFenotyp(oCechaShe.iFenotyp, oCechaShe.dGenotyp)

        If oCechaMe.iFenotyp = 0 Then Return dPrawdopShe  ' skoro ja jestem recesywny (genetycznie 0), to przyjmuję jej
        If oCechaShe.iFenotyp = 0 Then Return dPrawdopMe  ' skoro ona jestem recesywna (genetycznie 0), to przyjmuję moje

        Return 1 - ((1 - dPrawdopMe) * (1 - dPrawdopShe))

    End Function

    Public Shared Function CechaUDziecka(iCecha As Integer, sNazwaCechy As String, oMe As JednaOsoba, oShe As JednaOsoba) As JednaCecha
        Dim dPrawdop As Double = ListaOsob.PoliczCecheDziecka(iCecha, oMe, oShe)
        If dPrawdop = -1 Then Return Nothing

        Dim oRet As New JednaCecha
        oRet.sName = sNazwaCechy
        oRet.dGenotyp = dPrawdop
        Return oRet
    End Function

    ' *UWAGA! wymaga GetLangString!
    Public Shared Function CechyGrupaKrwi(oMe As JednaOsoba, oShe As JednaOsoba) As List(Of JednaCecha)
        ' override grupy krwi (z dwu na cztery)
        ' 00: grupa krwi A
        ' 01: grupa krwi B
        ' -> blood type 0, A, B, AB, id: 100, 101, 102, 103
        Dim dGrupaA As Double = ListaOsob.PoliczCecheDziecka(1, oMe, oShe)
        Dim dGrupaB As Double = ListaOsob.PoliczCecheDziecka(2, oMe, oShe)

        Dim oList As New List(Of JednaCecha)
        ' brak danych
        If dGrupaA = -1 Or dGrupaB = -1 Then Return oList
        ' *TODO* teoretycznie mogłoby być że jest znane jedno, a nie jest znane drugie, ale zakładam że to się nie zdarzy

        Dim oNew As New JednaCecha
        oNew.sName = GetLangString("cecha100dom")
        oNew.dGenotyp = (1 - dGrupaB) * (1 - dGrupaA)
        oList.Add(oNew)

        oNew = New JednaCecha
        oNew.sName = GetLangString("cecha101dom")
        oNew.dGenotyp = dGrupaA * (1 - dGrupaB)
        oList.Add(oNew)

        oNew = New JednaCecha
        oNew.sName = GetLangString("cecha102dom")
        oNew.dGenotyp = dGrupaB * (1 - dGrupaA)
        oList.Add(oNew)

        oNew = New JednaCecha
        oNew.sName = GetLangString("cecha103dom")
        oNew.dGenotyp = dGrupaB * dGrupaA
        oList.Add(oNew)


        Return oList
    End Function

    ' *UWAGA! wymaga GetLangString!
    Public Shared Function CechaUDziecka(iCecha As Integer, oMe As JednaOsoba, oShe As JednaOsoba) As JednaCecha
        Return CechaUDziecka(iCecha, App.GetCechaName(iCecha), oMe, oShe)
    End Function

    ' *UWAGA! wymaga GetLangString!
    Public Shared Function PoliczPrawdop(oMe As JednaOsoba, oShe As JednaOsoba) As List(Of JednaCecha)
        If oMe Is Nothing Or oShe Is Nothing Then Return Nothing

        Dim oList As List(Of JednaCecha) = ListaOsob.CechyGrupaKrwi(oMe, oShe)  ' może być pusta lista, ale na pewno będzie

        For iCecha As Integer = 3 To 99
            Dim oCecha As JednaCecha = ListaOsob.CechaUDziecka(iCecha, oMe, oShe)
            If oCecha IsNot Nothing Then oList.Add(oCecha)
        Next

        Return oList
    End Function
End Class

