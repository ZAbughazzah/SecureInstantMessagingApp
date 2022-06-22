Public Class Aunthentication

    Public ActiveUser As String

    Dim ServerAvailable As Boolean = False

    Private Sub OK_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OK.Click
        Dim connectionString As String = Form1.BaseConnectionString + " User ID=" + UsernameTextBox.Text + "; Password = " + PasswordTextBox.Text + ";"

        Form1.sqlConnection = New SqlClient.SqlConnection(connectionString)

        Me.OK.Enabled = False

        Try
            Form1.sqlConnection.Open()
            ActiveUser = UsernameTextBox.Text
            Me.Close()
            Form1.TabControl1.Enabled = True

        Catch ex As Exception
            'AuthenticationErrorMessage.ShowDialog()
            MessageBox.Show("Aunthentication failed! Either your username or password in incorrect.")
            Me.OK.Enabled = True
        End Try

        UsernameTextBox.Clear()
        PasswordTextBox.Clear()
    End Sub

    Private Sub Cancel_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Cancel.Click
        Me.Close()
        Form1.Close()
    End Sub

End Class
