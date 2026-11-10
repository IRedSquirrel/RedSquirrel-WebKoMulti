' GENERATED from a crop of the in-game Option › Display page (1:1 with the canvas). Pixel templates only.
' Option-window digits: 8 rows tall, left-aligned in a 7-wide cell (grayscale). Measured: 0 1 2 3 6 7 8. NOT measured: 4 5 9
' (no frame shows them) — an unknown glyph reads as '?' and the value is left alone. Also the 'ayar uygulandı.' line after Apply.
Namespace Engine.Login

    Friend Module GameSetupDigits

        Friend Const GlyphW As Integer = 7, GlyphH As Integer = 8

        Friend ReadOnly Glyphs As New Dictionary(Of Char, Byte()) From {
            {"1"c, Convert.FromBase64String("BCybBAQAAJu0mwADAAADKZsAAAAAAyubAAAAAAQsmwEBAAAELJsBAQAABCybAQEAAJu0tLRmAAA=")},
            {"6"c, Convert.FromBase64String("BASbtLRKAAObZwAAAABKgQABAAAAZ7S0tLQrAGdnAABImwBnZwEAALQALLQBAkqbAARKtLS0AgA=")},
            {"3"c, Convert.FromBase64String("BLS0tLQEACtnAABmgAADAgEAgGYAAwOBtGcBAAQEBAFmgQAEBAQEKpsAZ0oEBGeBACy0tLSbAgA=")},
            {"0"c, Convert.FromBase64String("BGe0tJsEACu0AQCAZgBnZwABKZsAZmYBACubAGdnAQIsmwBnZwECLJsAKrQBAoJnAAFntLSbAQA=")},
            {"2"c, Convert.FromBase64String("BLS0tJsEAEpnAACAZgADAgEASGYAAwMCAZtJAAQEBIJnAQAEBJtnAQIABLRKAQIBAGe0tLS0tAA=")},
            {"8"c, Convert.FromBase64String("BGe0tLQEAEqbAQBmgABKgQABSIAAAWe0tJsCACqCAQBmZgCBSgECALQAZ5sCAUqbAASBtLS0AgA=")},
            {"7"c, Convert.FromBase64String("Z7S0tLS0AAMBAABIgAADAwEAmykAAwMDSoEAAAQEBLQqAQAEBGeCAAIABAS0KgEAAASCggABAQA=")}
        }

        ''' <summary>"ayar uygulandı." (without the count) under Reset / Apply — crop 268,610; title corner 250,24 → +18,+586.</summary>
        Friend ReadOnly AppliedText As New GrayTemplate(102, 16, 268, 610,
            "ICAgHhweICAgHhweICAgHhweICAgHhweICAgHhweICAgHhweICAgHhweICAgHhweICAgHhweICAgHhweICAgHhweICAgHhweICAgHhweICAgHhweICAgHhweICAgHhweICAgHhweICAdHR0gICAdHR" &
            "0gICAdHR0gICAdHR0gICAdHR0gICAdHR0gICAdHR0gICAdHR0gICAdHR0gICAdHR0gICAdHR0gICAdHR0gICAdHR0gICAdHR0gICAdHR0gICAdHR0gICAdHR0gIB4cHiAgIB4cHiAgIB4cHiAgIB4c" &
            "HiAgIB4cHiAgIB4cHiAgIB4cHiAgIB4cHiAgIB4cHiAgIB4cHiAgIB4cHiAgYL6oHiAgIB4cHiAgIB4cHiAgIB4cHiAgeb6oHiAgIB4cHiAgHhwgICAgHhwgICAgHhwgICAgHhwgICAgHhwgICAgHh" &
            "wgICAgHhwgICAgHhwgICAgHhwgICAgHhwgICAgHhwgICAgXr6jBSAgHhwgICAgHhwgICAgHhwgICAgeb6jBSAgHhwgICAgHB8gICAfHB8gICAfHB8gICAfHB8gICAfHB8gICAfHB8gICAfHB8gICAf" &
            "HB8gICAfHB8gICAfHB8gICAfHB8gICAfXb6jAAUfHB8gICAfHB8gICAfHB8gICAfeL6jAAUfHB8gICAfXr6+vr6oHiC+vkQcHqi+eV++vr6+qB8cXr6+eb54HiAgIHm+qCBgvqgcvr5EIB+ovnkgkb" &
            "6+vr6oIHm+qCBgvqgcXr6jAE6+vr6+qB8ceb6okb6+QyAgkb6+vr6jAE6+qCAgIB8cHxMAAGy+iR+RvmwWRL6+AAwRAABsvokdX76+vr5sDR8fHXe+owRfvqMEkb5sFkC+vgBxvqQAbL6jBHe+owRf" &
            "vqMEX76jAAQRAABsvokdeb6+Tm++iBh5vqQAbL6jAE6+owQfHRsdH1++vr6+iAFdvqMAdr5sAABUvr6+vogBX76+AAAAAA0cHHi+owBOvqMATr6jAHW+bACjvmwBbL6jAG2+owBOvqMATr6jAARfvr" &
            "6+vogBeb6jAE2+owCnvmwBbL6jAE6+owAEHBwfeb6mAEy+iAAIpr4rpb4rAGy+pgBMvogAUb6+AAAAAA0cHXm+owBOvqMABKa+K6W+KwCjvkwAbb6jAG2+owBOvqMATr6jAG2+pgBMvogAb76jAEy+" &
            "owCkvkwAbb6jAE6+owAEHR8fqL5sAky+iAAJeb6IvokAAKW+bAJMvogAUb6+AAAfHRsfH3m+owBOvqMABHe+iL6JAAClvkwAcb6jAG2+owBOvqMATr6jAKS+bAJMvogAb76jAE6+owCkvkwAcb6jAE" &
            "6+owAEHx8fkL5sAIm+iAAIQr6+vkwBAI++bACJvogAUL6+AAAdGx0eHl6+vgBOvqMABEG+vr5MAQB1vogAcr6jAE6+vgBOvqMATr6jAIm+bACJvogAb76jAE6+owBtvogAcr6jAE6+owBtvqgdG76+" &
            "voy+iAAIHY++owAACBy+vr6MvogAUL6+AAAaGx0dHRylvr6+vqMABB2PvqMAAAgdpb6+vr6jAASlvr6+vqMATr6jAAS+vr6MvogAb76jAE6+owAEpb6+vr6jAE6+owBtvqMEHBwAAAAHAAAIG6e+TA" &
            "AAEBkaAAAABwAACBEAAAAaHBwcGxkaAgAAAAAABBynvkwAABEcGwEAiL5sAAMaAgAAAAAABBEAAAMaAAAABwAACAwAAAMPAAAEGwEAAAAAAAMPAAAECwAAGRkXAAAABwAHWr6+AAADFxcZGQAAAAcA" &
            "BxkOAAAZGRkXFxcZGQQAAAAABFu+vgAABBk9vr6+vmwAAAMZGQQAAAAABBkOAAMZGQAAAAcABxkKAAMZDwADFxcEAAAAAAMZDwADFwoAFhQUFRYWFhQUFQ0AAAAMFRYWFhQUFRYWFhQUFRYWFhQUFR" &
            "YWFhQUFRYWFhQMAAAADRQUEAAAAAADAAoWFhQUFRYWFhQUFRYWFhQUFRYWFhQUFRYWFhQUFRYWFhQUFRYWFhQUFRYWEA8REREREA8REREKAAAREREREA8REREREA8REREREA8REREREA8REREREA8R" &
            "CgAAEA8REQ0AAAAABxEREA8REREREA8REREREA8REREREA8REREREA8REREREA8REREREA8RERER")

    End Module

End Namespace
