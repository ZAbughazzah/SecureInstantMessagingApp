'Import the cryptography namespaces
Imports System.Security.Cryptography
Imports System.Text
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Globalization
Imports System.Threading
Imports System
Imports System.Data.SqlClient

Public Class Form1
    'Declare global variables
    ' Cloud database server parameters
    Public ServerName As String = "163.123.183.84,17183"
    Public BaseConnectionString As String = "Server=" + ServerName + ";Database=InstantMessaging; Trusted_Connection=No;MultipleActiveResultSets=true;"
    ' Credentials to be used for testing connecting to server i.e. to check is server is available
    Public ConTestUserName As String = "admin"
    Public ConTestPassword As String = "admiN#123"
    Public ServerAvailable As Boolean = False
    ' SQL connection variables 
    Public sqlConnection As SqlConnection
    Private myCmd As SqlCommand
    Private myReader As SqlDataReader
    ' Encryption an decryption variables
    Dim textbytes, encryptedtextbytes As Byte()
    Dim encoder As New UTF8Encoding
    Dim Reciever As String = ""
    ' Arraylists for storing UserIDs and their corresponding public keys for subscribers/contacts in the messaging service
    Dim SubscriberUserIDs As ArrayList = New ArrayList()
    Dim SubscriberPublicKeys As ArrayList = New ArrayList()
    Public counter As Integer = 0

    'done
    Private Function EncryptMessage(ByVal TextToEncrypt As String, ByVal PublicKey As String) As String
        'initialize encryptor with reciever's public key
        Dim Encryptor As New RSACryptoServiceProvider
        Encryptor.FromXmlString(PublicKey)
        'use utf8 to convert the text string into a byte array
        textbytes = encoder.GetBytes(TextToEncrypt)
        encryptedtextbytes = Encryptor.Encrypt(textbytes, True)
        'convert the encrypted byte array into a base64 string for display purposes
        'replace "" with the string to be returned by this function.
        Return (Convert.ToBase64String(encryptedtextbytes))
    End Function

    'Done
    Private Function DecryptMessage(ByVal TextToDecrypt As String, ByVal key As String) As String
        Dim Decryptor As New RSACryptoServiceProvider
        Decryptor.FromXmlString(key)
        encryptedtextbytes = Convert.FromBase64String(TextToDecrypt)
        Dim decryptedBytes As Byte()
        Try
            decryptedBytes = Decryptor.Decrypt(encryptedtextbytes, True)
            Return encoder.GetString(decryptedBytes)
        Catch ex As Exception
        End Try
        Return ""
    End Function

    'Done
    Private Sub SendMessage()
        'Insert Message to be sent into the Chat Window. The message should be in green and righ aligned.
        'Align the text in the center of the TextBox control.
        AppendSentMessage(InputMessageTextBox.Text)
        'Encrypt the message with public key of reciever and encrypt its copy with the public key of the sender. This makes it possible to decrypt both sent and recieved 
        ' messages on the chat window
        'ENCRYPT COPY WITH  PK OF SENDER
        Dim messageCopy As String = EncryptMessage(InputMessageTextBox.Text, PublicKeyTB.Text)
        Dim reciever As String = ContactsListBox.SelectedItem
        myCmd = sqlConnection.CreateCommand
        myCmd.CommandText = "select PublicKey from Subscribers where UserID = '" & reciever & "'"
        myReader = myCmd.ExecuteReader()
        myReader.Read()
        Dim recieverPk As String = myReader.GetString(0)
        Dim message As String = EncryptMessage(InputMessageTextBox.Text, recieverPk)
        Dim hashmessage As String = GetHash(InputMessageTextBox.Text)
        Dim hashmessageEnc As String = EncryptMessage(hashmessage, PrivateKeyTB.Text)
        LogMessage(Aunthentication.ActiveUser, reciever, message, messageCopy, hashmessageEnc, "sent")
        ' Clear Message imput textbox for the next message
        InputMessageTextBox.Clear()
    End Sub

    'Done
    Private Sub AppendSentMessage(ByVal m As String)
        'Insert Message to be sent into the Chat Window, align it to the right and in green color
        ChatMessages.SelectionColor = Color.Green
        ChatMessages.SelectionAlignment = HorizontalAlignment.Right
        ChatMessages.AppendText(m + $"{Environment.NewLine}")
    End Sub
    'Done
    Private Sub AppendRecievedMessage(ByVal m As String)
        '        ChatMessages.ForeColor = Color.Black
        ChatMessages.SelectionAlignment = HorizontalAlignment.Left
        'Insert Message recieved into the Chat Window, align it to the left in black color.
        ChatMessages.AppendText(m + $"{Environment.NewLine}")

    End Sub
    'Done
    Private Sub SendMessageButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SendMessageButton.Click
        'Clicki this button should invoke the SendMessage method. It should not be possible to send a message if not reciever is selected and if the message input box is empty.
        If (InputMessageTextBox.TextLength > 0) Then
            SendMessage()
        Else
            MessageBox.Show("Can not send empty message")
        End If

    End Sub
    'Done
    Public Function GetDateToday() As Date
        'Get today's date, formatted as yyyy-MM-dd
        Return (Format(Now, "yyyy-MM-dd"))
    End Function
    'Done
    Public Function GetTimeNow() As Date
        'Get the current time, formatted as HH:mm:ss        
        Return (Format(Now, "HH:mm:ss"))
    End Function

    'Done
    Public Sub LogMessage(ByVal Sender As String, ByVal Reciever As String, ByVal MessageContent As String, ByVal MessageContentCopy As String, ByVal MessageHash As String, ByVal Status As String)
        'Save the sent message to the database server.
        myCmd = sqlConnection.CreateCommand
        myCmd.CommandText = "select count(*) from Messages"
        myReader = myCmd.ExecuteReader()
        myReader.Read()
        counter = myReader.GetInt32(0)
        Dim SQLCommand As New SqlClient.SqlCommand
        SQLCommand.CommandText = "INSERT INTO Messages (MessageIdentityNumber, Sender, Reciever, MessageContent,
        MessageContentCopy, MessageHash,Date, Time, Status) VALUES
        ('" & CStr(counter) & "','" + Sender + "','" + Reciever + "','" + MessageContent + "','" + MessageContentCopy +
      "','" + MessageHash + "','" + GetDateToday() + "','" & GetTimeNow() & "','" + Status + "')"
        SQLCommand.Connection = sqlConnection
        SQLCommand.ExecuteNonQuery()
    End Sub
    'Done
    Public Sub ActivateUserAccount()
        ' Generate public and private keys
        Dim rsa As New RSACryptoServiceProvider
        Dim privateKey As String = rsa.ToXmlString(True)
        Dim publicKey As String = rsa.ToXmlString(False)
        PublicKeyTB.Text = publicKey
        PrivateKeyTB.Text = privateKey
        ' Store Public Key in the Cloud database and store Private Key in the device       
        Dim ID As String = Aunthentication.ActiveUser
        Dim f As System.IO.StreamWriter
        f = My.Computer.FileSystem.OpenTextFileWriter(ID + "privatekey.txt", False)
        f.WriteLine(privateKey)
        f.Close()
        Dim SQLCommand As New SqlClient.SqlCommand
        SQLCommand.Connection = sqlConnection
        SQLCommand.CommandText = "INSERT INTO Subscribers (UserID, PublicKey) VALUES ('" + ID + "','" + publicKey + "')"
        SQLCommand.ExecuteNonQuery()
        MessageBox.Show("Account Activated")
        ActivateAccount.Enabled = False
    End Sub
    'Done
    Private Function GetHash(ByVal Input As String) As String
        'Create the hash of the message
        Using hasher As MD5 = MD5.Create()
            ' Convert to byte array and get hash
            Dim inputbytes As Byte() = hasher.ComputeHash(Encoding.UTF8.GetBytes(Input))
            Return Convert.ToBase64String(inputbytes)
        End Using
    End Function
    'Done
    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        ' Create a temporary connection to Server
        sqlConnection = New SqlConnection(BaseConnectionString + " User ID=" + ConTestUserName + "; Password=" + ConTestPassword + ";")
        ' Use the temporal connection to check if Server is available.
        ' If Server is available display the Authentication screen to request the user for their credentials.
        ' Quit the application if Instant Messaging Server is not available.
        Try
            sqlConnection.Open()
            sqlConnection.Close()
            ServerAvailable = True
        Catch ex As Exception
            MessageBox.Show("Connection to Instant Messaging Server is NOT Available. Check to ensure that the Server is active and your computer has an internet connection.", "Connection Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

        Try
            If (ServerAvailable = True) Then
                Aunthentication.ShowDialog()
                Aunthentication.Focus()
                Aunthentication.UsernameTextBox.Clear()
                Aunthentication.PasswordTextBox.Clear()
                '---------------------
                'Retrieve all subscibers
                myCmd = sqlConnection.CreateCommand
                myCmd.CommandText = "SELECT * FROM Subscribers"
                myReader = myCmd.ExecuteReader()
                BusyMessageDisplay.Text = "Loading data.... Please wait!"
                BusyMessageDisplay.Show()
                ' Display the subscriber list in the Contacts ListBox.
                While (myReader.Read)
                    Dim SubscriberUserID As String = myReader.GetString(0)
                    Dim SubscriberPublicKey As String = myReader.GetString(1)
                    ContactsListBox.Items.Add(SubscriberUserID)
                    SubscriberUserIDs.Add(SubscriberUserID)
                    'MessageBox.Show(SubscriberUserID)
                    SubscriberPublicKeys.Add(SubscriberPublicKey)
                End While
                myReader.Close()
                BusyMessageDisplay.Close()
                'Check if the user account is activated. Prompt user to click the Activate Account button to activate the account.
                If Not (SubscriberUserIDs.Contains(Aunthentication.ActiveUser)) Then
                    TabControl1.SelectTab(1)
                    TabControl2.SelectTab(0)
                    MessageBox.Show("User account is NOT active. Click Activate Account to activate your acount.")
                Else
                    LoadKeys()
                End If
                ContactsListBox.SelectedIndex = 0
                ChatUpdateTimer.Enabled = True
            End If
        Catch ex As Exception

        End Try
    End Sub
    'Done
    Private Sub InputMessageTextBox_KeyPress(ByVal sender As System.Object, ByVal e As System.Windows.Forms.KeyPressEventArgs) Handles InputMessageTextBox.KeyPress
        'Pressing the ENTER key while the InputMessageTextBox has focuss, should have the same effect as clicking the Send Message button 
        Dim tmp As System.Windows.Forms.KeyPressEventArgs = e
        If tmp.KeyChar = ChrW(Keys.Enter) And InputMessageTextBox.Focused() = True Then
            SendMessageButton_Click(sender, e)
        End If

    End Sub

    'Done
    Private Sub RetrieveMessages(ByVal AlreadyRetrieved As Integer)
        Reciever = ContactsListBox.SelectedItem
        ' Query the database for messages that were either sent by this subscriber or sent to this subscriber 
        myCmd = sqlConnection.CreateCommand
        myCmd.CommandText = "select * from Messages where (Sender = '" + Aunthentication.ActiveUser + "'and 
        Reciever = '" + Reciever + "') or (Reciever = '" + Aunthentication.ActiveUser + "'and Sender = '" + Reciever + "')"
        myReader = myCmd.ExecuteReader()
        'MessageBox.Show("Retrieving Messages...")
        Dim counter As Int32 = 0
        Dim decryptedMessage As String
        Dim signed As String
        If AlreadyRetrieved <> 0 Then
            AlreadyRetrieved = AlreadyRetrieved - 1
        End If
        While (myReader.Read)
            If counter = AlreadyRetrieved Then '0
                'compare hash decMessage with hased one
                If myReader.GetString(1) = Aunthentication.ActiveUser Then
                    decryptedMessage = DecryptMessage(myReader.GetString(4), PrivateKeyTB.Text)
                    signed = DecryptMessage(myReader.GetString(5), PrivateKeyTB.Text)
                    If GetHash(decryptedMessage) <> signed Then
                        MessageBox.Show("OOPS...Message was tampered")
                    Else
                        AppendSentMessage(decryptedMessage)
                    End If
                ElseIf myReader.GetString(1) = Reciever Then
                    decryptedMessage = DecryptMessage(myReader.GetString(3), PrivateKeyTB.Text)
                    AppendRecievedMessage(decryptedMessage)
                End If
            ElseIf AlreadyRetrieved > counter Then
                AlreadyRetrieved = AlreadyRetrieved - 1
            End If
            'counter = counter + 1
        End While
        ' If message sent by this subscriber, append it as right justified on the chat window and color the message as green.
        ' If message was sent to this subscriber, append it as left justified on the chat window and color the text black.
        ' For all message (sent or received), use their hashes to check if they are not been tampered with.
    End Sub

    'done
    Private Sub ContactsListBox_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ContactsListBox.SelectedIndexChanged
        ' Set the selected contact as the current message reciever/recipient
        ' Clear the chat window, retrieve and display all chat messages between the selected contact and this subscriber
        ChatMessages.Clear()
        InputMessageTextBox.Clear()
        ContactsListBox_Click(sender, e)
        RetrieveMessages(0)
    End Sub
    'Done
    Private Sub ActivateAccount_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ActivateAccount.Click
        'If the subscriber account is not activated, call the method for activate it here. 
        sqlConnection = New SqlConnection(BaseConnectionString + " User ID=" + ConTestUserName + "; Password=" + ConTestPassword + ";")
        sqlConnection.Open()
        myCmd = sqlConnection.CreateCommand
        myCmd.CommandText = "select count(*) from Subscribers where UserID ='" & Aunthentication.ActiveUser & "'"
        myReader = myCmd.ExecuteReader()
        'If the account is already activated, display a message showing the account is already activated  
        myReader.Read()
        If (myReader.GetInt32(0) > 0) Then
            MessageBox.Show("Account Already Activated")
        Else
            ActivateUserAccount()
        End If
    End Sub
    'Done
    Private Sub LoadKeys()
        Dim fi As System.IO.StreamReader
        fi = My.Computer.FileSystem.OpenTextFileReader(Aunthentication.ActiveUser + "privatekey.txt")
        Dim privatekey As String
        Dim publickey As String
        privatekey = fi.ReadLine()
        fi.Close()
        PrivateKeyTB.Text = privatekey
        ActivateAccount.Enabled = False
        sqlConnection = New SqlConnection(BaseConnectionString + " User ID=" + ConTestUserName + "; Password=" + ConTestPassword + ";")
        sqlConnection.Open()
        myCmd = sqlConnection.CreateCommand
        myCmd.CommandText = "select PublicKey from Subscribers where UserID = '" & Aunthentication.ActiveUser & "'"
        Try
            myReader = myCmd.ExecuteReader()
            myReader.Read()
            publickey = myReader.GetString(0)
            PublicKeyTB.Text = publickey
        Catch ex As Exception

        End Try
    End Sub

    'Done
    Private Sub ContactsListBox_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ContactsListBox.Click
        'Initially the Send Message button is disabled. Add code here to enable the Send Message button to be enabled when a contact is selected.
        SendMessageButton.Enabled = Enabled
    End Sub

    'Done
    Private Sub ChatUpdateTimer_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ChatUpdateTimer.Tick
        ' Automatical retrieve new chat messages between the user and the selected subscriber from the server.
        ' Add the new messages to the chat window.
        'MessageBox.Show(ChatMessages.Lines.Count())
        'MessageBox.Show("Index:" + ContactsListBox.SelectedIndex.ToString)
        'MessageBox.Show("Item:" + ContactsListBox.SelectedItem.Length.ToString)
        'If (ContactsListBox.SelectedIndex >= 0 & ContactsListBox.SelectedItem.Length > 0) Then
        'MessageBox.Show(ChatMessages.Lines.Count())
        RetrieveMessages(ChatMessages.Lines.Count())
        'End If
    End Sub

    'Done
    Private Sub ChangePassword_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ChangePassword.Click
        ' If the subscriber has supplied the correct credentials to show that he/she is the owner of this account, update his password to the new one.
        If NewPassword.Text = RetypeNewPassword.Text Then
            Try
                Dim SQLCommand As New SqlClient.SqlCommand
                SQLCommand.CommandText = "ALTER LOGIN " + Aunthentication.ActiveUser + " with password= '" + NewPassword.Text + "' old_password = '" + OldPassword.Text + "'"
                SQLCommand.Connection = sqlConnection
                SQLCommand.ExecuteNonQuery()
                MessageBox.Show("Password changed Successfully")
            Catch ex As Exception
                MessageBox.Show("Unsuccessful password change... try again")
            End Try
        Else
            MessageBox.Show("New password does not match")
        End If
        OldPassword.Clear()
        NewPassword.Clear()
        RetypeNewPassword.Clear()
    End Sub
End Class