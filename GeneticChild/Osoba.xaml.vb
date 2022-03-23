
' tabelka edycyjna parametrów osoby
' po dodaniu osoby - wyliczanie prawdopodobieństwa w dół drzewka

' może być jakoś sprawdzanie, że nie ma sensu dalej wpisywać jakiejś cechy, bo jest już ustalona
'       (np. gdy jest grupa krwi 0, to nie ma sensu sprawdzać grupy krwi dalej w przodkach)
'       albo że coś się nie zgadza :)

Public NotInheritable Class Osoba
    Inherits Page

    Dim miInputParam As Integer = -10
    Dim moOsoba As JednaOsoba = Nothing

    Protected Overrides Sub onNavigatedTo(e As NavigationEventArgs)
        DumpCurrMethod()
        ' *CHECK* czy przy back jest to pamiętane?
        If e.Parameter Is Nothing Then Return
        miInputParam = CType(e.Parameter, Integer)
    End Sub

    Private Sub PokazDane()
        PokazGuzikiRodzicielskie()
        PokazPodstawoweDane()

        PokazComboListyCechFenotyp()
        PokazListeCechFenotyp()
        PokazListeWyliczeniowa()
    End Sub

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        DumpCurrMethod()

        If miInputParam = -10 Then
            DumpMessage("Ale nie wiem o jaką osobę chodzi, nie było parametru (wywołania onNavigatedTo)")
            Return
        End If

        moOsoba = Await GetOsobaDoEdycji(miInputParam)
        If moOsoba Is Nothing Then Return ' Me.Frame.GoBack()

        ' wysyłanie/wczytywanie robimy tylko dla pierwszego poziomu (chyba?)
        If miInputParam < 0 Then
            uiCmdBar.Visibility = Visibility.Visible
        Else
            uiCmdBar.Visibility = Visibility.Collapsed
        End If

        PokazDane()
    End Sub
    Private Async Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        DumpCurrMethod()

        Await ZapiszJesliZmiana()
        Me.Frame.GoBack()
    End Sub

    Private Async Sub uiTata_Click(sender As Object, e As RoutedEventArgs)
        DumpCurrMethod()

        Await ZapiszJesliZmiana()

        If moOsoba.iIdFather < 0 Then
            moOsoba.iIdFather = Await DodajOsobe()
        End If

        If moOsoba.iIdFather < 0 Then Return

        Me.Frame.Navigate(GetType(Osoba), moOsoba.iIdFather)

    End Sub

    Private Async Sub uiMama_Click(sender As Object, e As RoutedEventArgs)
        DumpCurrMethod()

        Await ZapiszJesliZmiana()

        If moOsoba.iIdMother < 0 Then
            moOsoba.iIdMother = Await DodajOsobe()
        End If

        If moOsoba.iIdMother < 0 Then Return

        Me.Frame.Navigate(GetType(Osoba), moOsoba.iIdMother)
    End Sub

    Private Sub PokazListeCechFenotyp()
        DumpCurrMethod()

        ' pokazanie w ListView tych, które są w moOsoba (zarówno recesywne jak i dominujące)

        Dim oLista As New List(Of CechaFenotypDoListy)

        ' *TODO* konwersja grupy krwi (ale dla dziecka jest konwersja zrobiona, więc tu może niekoniecznie)

        For Each oCecha As JednaCechaOsoby In moOsoba.lCechy
            If oCecha.iFenotyp > -1 Then
                Dim oNew As New CechaFenotypDoListy
                oNew.iId = oCecha.iIdCecha
                oNew.sNazwa = App.GetCechaName(oCecha.iIdCecha, (oCecha.iFenotyp = 1))
                oLista.Add(oNew)
            End If
        Next

        uiFenotypItems.ItemsSource = oLista
    End Sub


    Private Sub PokazComboListyCechFenotypKrew()
        For Each oCecha As JednaCechaOsoby In moOsoba.lCechy
            ' mamy fenotypowo coś o krwi
            If oCecha.iIdCecha > 99 Then Return
        Next

        For iLoop As Integer = 100 To 103
            Dim sDom As String = App.GetCechaName(iLoop)
            Dim oNew As New ComboBoxItem
            oNew.Content = sDom
            oNew.DataContext = iLoop
            uiComboCech.Items.Add(oNew)
        Next

    End Sub
    Private Sub PokazComboListyCechFenotyp()
        DumpCurrMethod()

        ' do combo idą tylko te, których jeszcze nie ma w moOsoba z fenotyp ustalonym

        uiComboCech.Items.Clear()

        ' nagłówkowe ("wybierz")
        Dim oNew As New ComboBoxItem
        oNew.Content = GetLangString("msgOsobaSelectCecha")
        oNew.DataContext = 0
        oNew.IsSelected = True
        uiComboCech.Items.Add(oNew)

        ' konwersja grupy krwi
        PokazComboListyCechFenotypKrew()

        ' od 3, bo pomijam grupy krwi
        For iLoop As Integer = 3 To 99
            Dim bMamy As Boolean = False
            For Each oCecha As JednaCechaOsoby In moOsoba.lCechy
                ' mamy taką cechę fenotypowo
                If oCecha.iIdCecha = iLoop And oCecha.iFenotyp > -1 Then
                    bMamy = True
                    Exit For
                End If
            Next

            ' zabezpieczenie: cechy usuwane (wykomentowane) nie mogą się wszak pojawić
            Dim sDom As String = App.GetCechaName(iLoop)
            Dim sRec As String = App.GetCechaName(iLoop, False)
            If sDom.Length > 4 And sRec.Length > 4 Then
                If Not bMamy Then
                    oNew = New ComboBoxItem
                    oNew.Content = sDom
                    oNew.DataContext = iLoop
                    uiComboCech.Items.Add(oNew)
                    oNew = New ComboBoxItem
                    oNew.Content = sRec
                    oNew.DataContext = iLoop + 1000
                    uiComboCech.Items.Add(oNew)
                End If
            End If
        Next
    End Sub

    Private Sub PrzeliczCechyWDrzewie(oOsoba As JednaOsoba)
        DumpCurrMethod()

        App.glItems.PrzeliczCechyWDrzewku(oOsoba.iId)
    End Sub
    Private Sub uiCechaAdd_Click(sender As Object, e As RoutedEventArgs)
        DumpCurrMethod()

        ' dodanie do moOsoba cechy z ComboBox

        Dim oSelItem As ComboBoxItem = uiComboCech.SelectedItem
        If oSelItem Is Nothing Then Return

        Dim iCechaId As Integer = CType(oSelItem.DataContext, Integer)
        If iCechaId = 0 Then Return ' wybrane jest "wybierz" :)

        Dim bDominujaca As Boolean = True
        If iCechaId > 1000 Then
            bDominujaca = False
            iCechaId -= 1000
        End If

        ' grupa krwi do przerobienia; 90-93
        If iCechaId >= 100 Then
            Select Case iCechaId
                Case 100
                    ' grupa 0
                    DodajCeche(1, False)
                    DodajCeche(2, False)
                Case 101
                    ' grupa A
                    DodajCeche(1, True)
                    DodajCeche(2, False)
                Case 102
                    ' grupa B
                    DodajCeche(1, False)
                    DodajCeche(2, True)
                Case 103
                    ' grupa AB
                    DodajCeche(1, True)
                    DodajCeche(2, True)
            End Select
        Else
            DodajCeche(iCechaId, bDominujaca)
        End If


        App.glItems.MakeDirty()

        PrzeliczCechyWDrzewie(moOsoba)
        PokazComboListyCechFenotyp()
        PokazListeCechFenotyp()
        PokazListeWyliczeniowa()
    End Sub

    Private Sub DodajCeche(iCechaId As Integer, bDominujaca As Boolean)
        ' tylko do wywolywania "opakowane" przez UI_Click; wydzielone bo zamiana grupy krwi tego wymaga

        Dim iFenotyp As Integer = 0
        If bDominujaca Then iFenotyp = 1

        Dim bMam As Boolean = False
        For Each oItem As JednaCechaOsoby In moOsoba.lCechy
            ' żeby zachować dGenotyp
            If iCechaId = oItem.iIdCecha Then
                oItem.iFenotyp = iFenotyp
                bMam = True
                Exit For
            End If
        Next

        If Not bMam Then
            Dim oNew As New JednaCechaOsoby
            oNew.iFenotyp = iFenotyp
            oNew.iIdCecha = iCechaId
            moOsoba.lCechy.Add(oNew)
        End If

    End Sub
    Private Sub uiDelCecha_Click(sender As Object, e As RoutedEventArgs)
        DumpCurrMethod()

        ' usuniecie cechy DataContext z listy w moOsoba

        Dim oFE As FrameworkElement = TryCast(sender, FrameworkElement)
        If oFE Is Nothing Then Return

        Dim oCecha As CechaFenotypDoListy = TryCast(oFE.DataContext, CechaFenotypDoListy)
        If oCecha Is Nothing Then Return

        For Each oItem As JednaCechaOsoby In moOsoba.lCechy
            If oCecha.iId = oItem.iIdCecha Then
                oItem.iFenotyp = -1
                Exit For
            End If
        Next

        App.glItems.MakeDirty()

        PrzeliczCechyWDrzewie(moOsoba)
        PokazComboListyCechFenotyp()
        PokazListeCechFenotyp()
        PokazListeWyliczeniowa()
    End Sub

    Private Async Function ZapiszJesliZmiana() As Task
        DumpCurrMethod()

        If CheckDataModified() Then
            If Await DialogBoxResYNAsync("msgOsobaDataChanged") Then
                ZapiszPodstawoweDane()
            End If
        End If
        App.glItems.Save(False)
    End Function

    Private Sub PokazListeWyliczeniowa()
        DumpCurrMethod()

        Dim mCechy As New List(Of JednaCecha)

        ' konwersja z moOsoba do mCechy
        For Each oItem As JednaCechaOsoby In moOsoba.lCechy
            Dim oNew As New JednaCecha
            oNew.sName = App.GetCechaName(oItem.iIdCecha)
            If oNew.sName.Length < 4 Then Continue For
            oNew.dGenotyp = oItem.dGenotyp
            mCechy.Add(oNew)
        Next
        ' ewentualnie z pomijaniem tych, które są fenotypowo?

        ' pokazanie
        uiGenotypItems.ItemsSource = mCechy
    End Sub

    Private Sub PokazPodstawoweDane()
        DumpCurrMethod()

        uiName.Text = moOsoba.sName
        'uiDatUr.Date = moOsoba.dUrodziny
        uiNotes.Text = moOsoba.sNote
    End Sub

    Private Function CheckDataModified() As Boolean
        DumpCurrMethod()

        If uiName.Text <> moOsoba.sName Then Return True
        'If uiDatUr.Date <> moOsoba.dUrodziny Then Return True
        If uiNotes.Text <> moOsoba.sNote Then Return True
        Return False
    End Function

    Private Sub ZapiszPodstawoweDane()
        DumpCurrMethod()

        moOsoba.sName = uiName.Text
        'If uiDatUr.Date IsNot Nothing Then moOsoba.dUrodziny = uiDatUr.Date.Value
        moOsoba.sNote = uiNotes.Text
        App.glItems.MakeDirty()
    End Sub

    Private Sub PokazGuzikiRodzicielskie()
        DumpCurrMethod()

        If moOsoba.iIdFather > 0 Then
            uiTata.Content = App.glItems.ZnajdzOsobaId(moOsoba.iIdFather)?.sName
        Else
            uiTata.Content = GetLangString("msgOsobaTataAdd")
        End If

        If moOsoba.iIdMother > 0 Then
            uiMama.Content = App.glItems.ZnajdzOsobaId(moOsoba.iIdMother)?.sName
        Else
            uiMama.Content = GetLangString("msgOsobaMamaAdd")
        End If
    End Sub

    Private Async Function DodajOsobe(Optional iTyp As Integer = -1) As Task(Of Integer)
        DumpCurrMethod()

        Dim oNew As New JednaOsoba
        oNew.iTyp = iTyp
        oNew.sName = Await DialogBoxInputResAsync("msgOsobaEnterName")
        If oNew.sName = "" Then Return -1

        ' wpisanie wszystkich cech jako unknown - będzie potrzebne do liczenia w drzewie przecież
        For iLoop = 1 To 99
            If App.GetCechaName(iLoop).Length < 4 Then Continue For
            Dim oNewCecha As New JednaCechaOsoby
            oNewCecha.iIdCecha = iLoop
            oNew.lCechy.Add(oNewCecha)
        Next

        Dim iId As Integer = App.glItems.Add(oNew)
        App.glItems.Save(False)
        Return iId
    End Function

    Private Async Function GetOsobaDoEdycji(iInputParam As Integer) As Task(Of JednaOsoba)
        DumpCurrMethod()

        Dim oRet As JednaOsoba = Nothing

        ' skoro dodajemy tylko w górę, to nie ma powodu nic wyliczać przy dodawaniu osoby - tylko przy dodawaniu cech

        If miInputParam > 0 Then
            ' edycja osoby o takim Id
            oRet = App.glItems.ZnajdzOsobaId(miInputParam)
            If oRet Is Nothing Then
                Await DialogBoxAsync("Błąd wywołania, takiej osoby nie ma!")
                Return Nothing
            End If
        Else
            ' edycja osoby o typie -miInputPatam, albo jej dodanie
            oRet = App.glItems.ZnajdzOsobaTyp(-miInputParam)
            If oRet Is Nothing Then
                If Not Await DialogBoxResYNAsync("msgOsobaWantAddYourself") Then Return Nothing

                Dim iNewItem = Await DodajOsobe(-miInputParam)
                If iNewItem < 0 Then Return Nothing
                miInputParam = iNewItem
                Return App.glItems.ZnajdzOsobaId(miInputParam)
            End If
        End If

        Return oRet
    End Function

    Private Async Sub uiSendData_Click(sender As Object, e As RoutedEventArgs)

        ' If Not App.glItems.CzyJestOsobaTyp(1) Then Return

        Dim oMsg As Email.EmailMessage = New Windows.ApplicationModel.Email.EmailMessage()
        oMsg.Subject = GetLangString("msgOsobaEmailSubject") & " " & moOsoba.sName

        Dim sTxt As String = GetLangString("msgOsobaEmailBodyHdr") & vbCrLf & vbCrLf &
            "Data: " & Date.Now & vbCrLf & vbCrLf & App.glItems.DumpData(moOsoba)

        oMsg.Body = sTxt

        Await Email.EmailManager.ShowComposeNewEmailAsync(oMsg)

    End Sub

    Private Async Function InputDialogMultiline(sHeader As String) As Task(Of String)
        Dim oTBox As New TextBox
        oTBox.Header = sHeader
        oTBox.AcceptsReturn = True
        oTBox.Width = Me.Width * 0.75
        oTBox.Height = Me.Height * 0.5
        Dim oDlg As New ContentDialog
        oDlg.Content = oTBox
        oDlg.CloseButtonText = "OK"
        oDlg.Width = Me.Width * 0.8
        oDlg.Height = Me.Height * 0.6

        Dim oRes As ContentDialogResult = Await oDlg.ShowAsync

        Return oTBox.Text

    End Function

    Private Async Sub uiImportData_Click(sender As Object, e As RoutedEventArgs)

        If moOsoba.lCechy.Count > 0 Then
            If Not Await DialogBoxResYNAsync("msgCechyAlreadyExist") Then Return
            moOsoba.lCechy.Clear()
        End If

        Dim sTxt As String = Await InputDialogMultiline(GetLangString("msgPasteData"))
        If sTxt.Length < 10 Then Return ' puste

        If Not Await ParseCopiedData(sTxt) Then
            DialogBoxRes("msgBadInputData")
        Else
            PokazDane()
        End If

    End Sub

    Private Async Function ParseCopiedData(sTxt As String) As Task(Of Boolean)
        ' ret: true gdy wczytany, false: gdy się nie udało

        ' 'odwrócenie uiSendData_Click
        Dim iInd As Integer = sTxt.IndexOf("Data: 20")  ' no bo data raczej ma rok 2000 :)
        If iInd < 1 Then Return False

        iInd = sTxt.IndexOf(vbCrLf, iInd)
        sTxt = sTxt.Substring(iInd)

        ' odwrócenie App.glItems.DumpData(moOsoba)
        Dim aArr As String() = sTxt.Split(vbCrLf)
        If aArr.Length < 3 Then Return False

        If aArr(1).Trim.Length < 1 And Not aArr(0).Contains(vbTab) Then
            ' podejrzewam że to imię:
            ' sRet = oOsoba.sName & vbCrLf & vbCrLf
            If moOsoba.sName.ToLower <> aArr(0).ToLower Then
                ' próba podmiany imienia możliwa jest tutaj, stąd ASYNC (bo DlgBoxAsync)
            End If
        End If

        Dim lCechy As New List(Of JednaCechaOsoby)

        For Each sLine As String In aArr
            ' sRet = sRet & oCecha.iIdCecha & vbTab & oCecha.iFenotyp & vbTab & oCecha.dGenotyp & vbCrLf
            Dim aLine As String() = sLine.Split(vbTab)
            If aLine.Length <> 3 Then Continue For

            Dim oNew As New JednaCechaOsoby
            If Not Integer.TryParse(aLine(0), oNew.iIdCecha) Then Return False
            If Not Integer.TryParse(aLine(1), oNew.iFenotyp) Then Return False
            If Not Double.TryParse(aLine(2), oNew.dGenotyp) Then Return False

            lCechy.Add(oNew)
        Next

        ' przetworzone całe, nie ma błędów, więc zapisujemy
        moOsoba.lCechy = lCechy

        Return True
    End Function


End Class

Public Class CechaFenotypDoListy
    Public Property sNazwa As String
    Public Property iId As Integer
End Class