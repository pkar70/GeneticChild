' POMYSŁ: 2022.02.21

' problemy: dopisujemy (w nowszej wersji app) cechę, i co wtedy? SetSettings numer wersji, i jeśli na starcie GetSettings ma niższy numer, to wtedy kontrola i ewentualnie dopytanie o nową cechę?
' albo nie numer wersji, tylko policzenie cech w GetLang (dokąd sięga), i jeśli liczba inna niż znana...

' początek: wpisuje się siebie, swoje cechy widoczne.
' grupa krwi: ewentualnie osobno zrobiona, bo są dwie dominujące i jedna recesywna

' cechy:
' 1) tak/nie, proste binarne (np. kręcone włosy)
' 2) sekwencja, czyli krótkowidz, normalny, dalekowidz
' 3) dwa dominujące, recesywna (np. grupa krwi, A, B, 0). Może być "emulowane" na dwu cechach binarnych (translacja storage/display)

' larum dla niektórych cech (np. konflikt serologiczny); to może być w kodzie
' delete spouse

' MainPage: ikonki buttonów: ludzik, pierścionek, niemowlę w beciku
' Osoba: ikonki buttonów: mężczyzna, kobieta (jako rodzice)

' cecha krótsza niż 5 znaków: do pomijania (empty)


Public NotInheritable Class MainPage
    Inherits Page

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        DumpCurrMethod()
        ' policz znane cechy, napisz nr wersji
        Dim iCechyCnt As Integer = PoliczZnaneCechy()
        uiCechaCount.Text = GetLangString("msgLiczbaCech") & ": " & iCechyCnt


        App.glItems.Load()  ' ale w nim jest że nie wczytuje gdy ma wczytane

        ' ewentualnie zablokuj guzik child, z dodaniem ToolTip ze najpierw trzeba dodac me/spouse
        MozeZablokujChild()

        '' nie ma sensu wysyłanie danych, jeśli nie mamy danych o samym sobie
        ' REM, bo mail danych nie stąd (nie znamy płci usera)
        'uiMail.IsEnabled = App.glItems.CzyJestOsobaTyp(1)

        ' REM, bo są ikonki a nie imiona
        'Dim oShe As JednaOsoba = App.glItems.ZnajdzOsobaTyp(2)
        'If oShe IsNot Nothing Then
        '    uiShe.Content = oShe.sName
        '    Dim iInd As Integer = oShe.sName.IndexOf(" ")
        '    If iInd > 0 Then uiShe.Content = oShe.sName.Substring(0, iInd)
        'End If

        '' jesli nie ma ciebie, to dodaj siebie
        ' REM, bo nie znamy płci, więc nie wiemy czy 1 czy 2
        'MozePrzejdzDodajSiebie()
    End Sub

    Private Sub uiMe_Clicked(sender As Object, e As RoutedEventArgs)
        DumpCurrMethod()

        Me.Frame.Navigate(GetType(Osoba), -1)
    End Sub

    Private Sub uiShe_Clicked(sender As Object, e As RoutedEventArgs)
        DumpCurrMethod()

        ' REM, bo nie wiemy czy to spouse czy user
        'If Not App.glItems.CzyJestOsobaTyp(2) Then
        '    If Not Await DialogBoxYNAsync("Dodać spouse?") Then Return
        'End If
        Me.Frame.Navigate(GetType(Osoba), -2)
    End Sub

    Private Sub uiChild_Clicked(sender As Object, e As RoutedEventArgs)
        DumpCurrMethod()

        Me.Frame.Navigate(GetType(Dziecko))
    End Sub

    Private Function PoliczZnaneCechy() As Integer
        DumpCurrMethod()

        Dim iLoop As Integer = 1
        Dim iCnt As Integer = 0
        While iLoop < 100 ' > 90 są 'override'
            Dim sDom As String = App.GetCechaName(iLoop)
            Dim sRec As String = App.GetCechaName(iLoop, False)

            ' puste mogą być, po renumber tak właśnie jest!
            'If sDom = "ERR" Then Exit While
            'If sRec = "ERR" Then Exit While

            ' ale jeśli Len<5, to znaczy że to jest empty (usunięte)
            If sDom.Length > 4 And sRec.Length > 4 Then iCnt += 1

            iLoop += 1
        End While

        ' SetSettingsInt("liczbaCech", iLoop) ' ale razem z pustymi

        DumpMessage("Doliczyłem się cech: " & iLoop & ", a niepustych: " & iCnt)
        Return iCnt - 1 ' aby wyszło zero na koniec
    End Function

    Private Sub MozeZablokujChild()
        DumpCurrMethod()

        Dim bMe As Boolean = App.glItems.CzyJestOsobaTyp(1)
        Dim bShe As Boolean = App.glItems.CzyJestOsobaTyp(2)

        If bMe And bShe Then
            DumpMessage("- mogę policzyć, bo oboje rodzice są znani")
            uiChild.IsEnabled = True
            ToolTipService.SetToolTip(uiChild, GetLangString("msgToolTipChildPrawdop"))
            Return
        End If

        DumpMessage("- blokuję liczenie dziecka, bo nie znam rodziców")

        Dim sTmp As String = ""
        If Not bMe Then sTmp = GetLangString("msgToolTipChildMan")  ' "swoje"
        If Not bShe Then
            If sTmp = "" Then
                sTmp = GetLangString("msgToolTipChildWoman") ' "współmałżonka"
            Else
                sTmp = GetLangString("msgToolTipChildParents") ' "swoje i współmałżonka"
            End If
        End If
        uiChild.IsEnabled = False
        ToolTipService.SetToolTip(uiChild, GetLangString("msgToolTipChildNoData") & " " & sTmp)
    End Sub

    'Private Sub MozePrzejdzDodajSiebie()
    '    DumpCurrMethod()

    '    If App.glItems.CzyJestOsobaTyp(1) Then Return
    '    DumpMessage("Nie mam siebie, trzeba dodać")
    '    uiMe_Clicked(Nothing, Nothing)
    'End Sub

End Class
