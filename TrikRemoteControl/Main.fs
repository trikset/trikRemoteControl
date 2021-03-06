﻿namespace Main
open System
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Markup

open TrikRemoteControl


type MainWindow () as this =
    inherit Window()
    
    [<Literal>]
    let port = 8888

    let client =  new Client()
    
    do this.Loaded.Add <| fun _ -> 
        let button name = this.FindName name :?> Button
        let ipTextBox = this.FindName "ipTextBox" :?> TextBox
        let connectButton = this.FindName "connect" :?> ToggleButton
        let connectionFailedLabel = this.FindName "connectionFailedLabel" :?> Label
        let actions = ["up", Input.Key.Up; "down", Input.Key.Down; "left", Input.Key.Left; "right", Input.Key.Right; "stop", Input.Key.Back]
        let buttons = List.map (button << fst) actions

        let settings = TrikSettings ()        

        let setButtonsEnabled enabled = buttons |> List.iter (fun x -> x.IsEnabled <- enabled)

        let initHandlers () =
            let noConnection reason _ = 
                client.Disconnect()
                connectButton.IsChecked <- Nullable false
                connectButton.IsEnabled <- true
                connectionFailedLabel.Content <- reason
                connectionFailedLabel.Visibility <- Visibility.Visible
                setButtonsEnabled false
        
            let connectionSucceed _ =
                setButtonsEnabled true
                connectionFailedLabel.Visibility <- Visibility.Hidden
                connectButton.IsEnabled <- false

            client.ConnectedEvent.Add connectionSucceed 
            client.ConnectionFailedEvent.Add <| noConnection "Подключение не удалось"
            client.DisconnectedEvent.Add <| noConnection "Соединение потеряно"        

        let sendCommand command = client.Send <| "direct:" + command

        let init () =
            let registerButtonHandler buttonName handler =
                let button = this.FindName buttonName :?> ButtonBase
                Event.add handler button.Click

            let command script =
                let commandScriptFileName = "scripts/" + script + ".qts"
                let eventHandler _ = sendCommand <| System.IO.File.ReadAllText commandScriptFileName
                eventHandler

            let registerCommand (button, key) =
                registerButtonHandler button (command button)
                this.KeyDown 
                |> Event.filter (fun event -> event.Key = key) 
                |> Event.add (command button)

            let registerCustomKey (key, onKeyDownScript, onKeyUpScript) =
                this.KeyDown 
                |> Event.filter (fun event -> event.Key = key) 
                |> Event.add (command onKeyDownScript)
                this.KeyUp
                |> Event.filter (fun event -> event.Key = key) 
                |> Event.add (command onKeyUpScript)

            registerButtonHandler "connect" (fun _ ->                     
                    settings.IpAddress <- ipTextBox.Text
                    client.Connect(settings.IpAddress, port)
                )

            List.iter registerCommand actions

            setButtonsEnabled false
            
            buttons |> List.iter (fun x -> x.IsTabStop <- false)

            [Input.Key.Space, "onSpaceDown", "onSpaceUp"] |> List.iter registerCustomKey

            ipTextBox.Text <- settings.IpAddress
            ipTextBox.IsTabStop <- false
            initHandlers ()

        do init ()

module Main =
    [<STAThread>]
    [<EntryPoint>]
    let main _ = 
        let application = Application.LoadComponent (new Uri ("App.xaml", UriKind.Relative)) :?> Application
        application.Run ()
