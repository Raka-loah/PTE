Imports System.ComponentModel
Imports System.Text.RegularExpressions

Public Class Form1
    Dim last_music_title As String = ""
    Dim last_write As String = ""
    Dim proc_title As String = ""
    Dim music_players As New Dictionary(Of String, String)
    Dim processes As Process()
    Dim marquee_index As Integer = 0

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        music_players.Add("网易云音乐", "cloudmusic")
        music_players.Add("QQ音乐", "qqmusic")
        music_players.Add("Foobar2000", "foobar2000")
        music_players.Add("其他（右侧填写）", My.Settings.CustomExecutable)
        ComboBox1.DisplayMember = "Key"
        ComboBox1.ValueMember = "Value"
        ComboBox1.DataSource = New BindingSource(music_players, Nothing)
        ComboBox1.SelectedIndex = My.Settings.SelectedIndex
        Timer2.Interval = NumericUpDown2.Value
        If My.Settings.OutputFilePath = "music_title.txt" Then
            TextBox5.Text = IO.Path.Combine(IO.Path.GetDirectoryName(Application.ExecutablePath), "music_title.txt")
        End If
    End Sub

    ''' <summary>
    ''' Extract info from given window title.
    ''' </summary>
    ''' <param name="exe_title">Window title</param>
    ''' <param name="pattern">Pattern using %description% format</param>
    ''' <returns>Dictionary of %description%: matched content</returns>
    Private Function get_exe_title(exe_title As String, pattern As String) As Dictionary(Of String, String)
        Dim dic As New Dictionary(Of String, String)
        'Convert raw pattern string
        Dim converted_pattern As String = Regex.Replace(Regex.Escape(pattern), "\%(.+?)\%", "(?<$1>.+)", RegexOptions.IgnoreCase)
        Dim re = New Regex(converted_pattern)
        Dim results = re.Match(exe_title)
        If results.Success Then
            For i = 1 To results.Groups().Count - 1
                dic.Add(results.Groups(i).Name, results.Groups(i).Value)
            Next
        End If
        get_exe_title = dic
    End Function

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If TypeOf sender Is Timer Then
            Timer1.Enabled = False
        Else
            Timer1.Enabled = Not Timer1.Enabled
        End If
        TextBox2.Enabled = Not Timer1.Enabled
        TextBox3.Enabled = Not Timer1.Enabled
        TextBox4.Enabled = Not Timer1.Enabled
        Button1.Text = If(Timer1.Enabled, "⏹ 停止", "▶ 启动")
        If Timer1.Enabled Then
            processes = Process.GetProcessesByName(TextBox1.Text)
        End If
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Try
            If processes.Count = 0 Then
                Throw New ArgumentException("No such process")
            End If
            For Each proc As Process In processes
                proc.Refresh()
                If (proc.MainWindowTitle.Length > 0) Then
                    proc_title = proc.MainWindowTitle
                    Exit For
                End If
            Next
        Catch ex As Exception
            Debug.Print(ex.Message)
            TextBox4.Text = ""
            last_music_title = ""
            last_write = ""
            Call Button1_Click(Timer1, New EventArgs)
            Exit Sub
        End Try
        'Debug.Print(proc_title)
        Try
            Dim dic As Dictionary(Of String, String) = get_exe_title(proc_title, TextBox2.Text)
            Dim result = TextBox3.Text
            For Each k In dic.Keys
                result = result.Replace("%" & k & "%", dic(k)) '*giggles*
            Next
            If Not Timer2.Enabled Then
                TextBox4.Text = If(result.Length > NumericUpDown1.Value, result.Substring(0, NumericUpDown1.Value), result)
            End If
            If last_music_title <> result Then
                marquee_index = 0
                last_music_title = result
                If Not Timer2.Enabled And last_write <> TextBox4.Text Then
                    Try
                        IO.File.WriteAllText(TextBox5.Text, TextBox4.Text)
                        last_write = TextBox4.Text
                    Catch ex As Exception
                        MsgBox(ex.Message, vbOKOnly + vbCritical, "错误")
                        Timer2.Enabled = False
                        CheckBox1.Checked = False
                        TextBox4.Text = ""
                        last_music_title = ""
                        last_write = ""
                        Call Button1_Click(Timer1, New EventArgs)
                        Exit Sub
                    End Try
                End If
            End If
        Catch ex As Exception
            TextBox4.Text = ""
        End Try
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged
        TextBox1.Text = ComboBox1.SelectedValue
    End Sub

    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged
        If ComboBox1.SelectedIndex = ComboBox1.Items.Count - 1 Then
            My.Settings.CustomExecutable = TextBox1.Text
            My.Settings.Save()
        End If
    End Sub

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        My.Settings.SelectedIndex = ComboBox1.SelectedIndex
        My.Settings.Save()
    End Sub

    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        If marquee_index > last_music_title.Length - NumericUpDown1.Value Then marquee_index = 0
        Try
            If TextBox4.Text.Length >= NumericUpDown1.Value Then
                TextBox4.Text = last_music_title.Substring(marquee_index, Math.Min(NumericUpDown1.Value, last_music_title.Length))
                marquee_index += 1
            Else
                TextBox4.Text = last_music_title.Substring(0, Math.Min(NumericUpDown1.Value, last_music_title.Length))
            End If
        Catch ex As Exception
            Debug.Print(ex.Message)
        End Try
        Try
            IO.File.WriteAllText(TextBox5.Text, TextBox4.Text)
            last_write = TextBox4.Text
        Catch ex As Exception
            MsgBox(ex.Message, vbOKOnly + vbCritical, "错误")
            Timer2.Enabled = False
            CheckBox1.Checked = False
        End Try
    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        Timer2.Enabled = CheckBox1.Checked
    End Sub

    Private Sub NumericUpDown2_ValueChanged(sender As Object, e As EventArgs) Handles NumericUpDown2.ValueChanged
        Timer2.Interval = NumericUpDown2.Value
    End Sub

    Private Sub LinkLabel1_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel1.LinkClicked
        Process.Start("https://lotc.cc/process-title-extractor.wtf")
    End Sub

    Private Sub ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem1.Click
        If MsgBox("是否将所有设置重置为默认值？", vbYesNo + vbQuestion, "确认") = vbYes Then
            My.Settings.Reset()
            My.Settings.Save()
            Application.Restart()
        End If
    End Sub

    Private Sub TextBox5_Click(sender As Object, e As EventArgs) Handles TextBox5.Click
        If FolderBrowserDialog1.ShowDialog() = DialogResult.OK Then
            TextBox5.Text = IO.Path.Combine(FolderBrowserDialog1.SelectedPath, "music_title.txt")
        End If
    End Sub

    Private Sub 复制输出文件路径ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 复制输出文件路径ToolStripMenuItem.Click
        Try
            Clipboard.SetText(TextBox5.Text)
        Catch ex As Exception
            Debug.Print(ex.Message)
        End Try
    End Sub

    Private Sub 打开输出文件夹ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 打开输出文件夹ToolStripMenuItem.Click
        Process.Start(IO.Path.GetDirectoryName(TextBox5.Text))
    End Sub
End Class
