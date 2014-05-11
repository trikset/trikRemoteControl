namespace Main

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Controls.Primitives
open System.Windows.Markup

open TrikRemoteControl

/// Code behind for main window.
type MainWindow () as this =
    inherit Window()
    
    [<Literal>]
    let port = 8888

    /// Network communication client.
    let client =  new Client()
    
    /// Initialization of a form when it is loaded. There we subscrib to all events.
    do this.Loaded.Add <| fun _ -> 

        // Here are some helper functions that allow to quickly find control on a form.
        let button name = this.FindName name :?> Button
        let ipTextBox = this.FindName "ipTextBox" :?> TextBox
        let connectButton = this.FindName "connect" :?> ToggleButton
        let connectionFailedLabel = this.FindName "connectionFailedLabel" :?> Label
        
        // Here are a list of button names with their hotkeys.
        let actions = [
            "forward", Input.Key.Up; 
            "back", Input.Key.Down; 
            "left", Input.Key.Left; 
            "right", Input.Key.Right; 
            "stop", Input.Key.Back; 
            "manipulatorOpen", Input.Key.Insert;
            "manipulatorClose", Input.Key.Home;
            "manipulatorLeft", Input.Key.Delete;
            "manipulatorRight", Input.Key.End;
            ]

        // A list of all available buttons.
        let buttons = List.map (button << fst) actions

        // Persistent settings.
        let settings = TrikSettings ()        

        // Helper function that allows to enable or disable all buttons.
        let setButtonsEnabled enabled = buttons |> List.iter (fun x -> x.IsEnabled <- enabled)

        // Init handlers for network events.
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

        // Helper function for sending command to a robot.
        let sendCommand command = client.Send <| "direct:" + command

        // Adds handlers for buttons, initializes form.
        let init () =
            // Helper for registering handler to a button with given name.
            let registerButtonHandler buttonName handler =
                let button = this.FindName buttonName :?> ButtonBase
                Event.add handler button.Click

            // Helper to get handler that executes given script.
            let command script =
                let commandScriptFileName = "scripts/" + script + ".qts"
                let eventHandler _ = sendCommand <| System.IO.File.ReadAllText commandScriptFileName
                eventHandler

            // Registers button and hotkey event handler.
            let registerCommand (button, key) =
                registerButtonHandler button (command button)
                this.KeyDown 
                |> Event.filter (fun event -> event.Key = key) 
                |> Event.add (command button)

            // Registers keyboard key handler.
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
        // Loads application from XAML and lauches it.
        let application = Application.LoadComponent (new Uri ("App.xaml", UriKind.Relative)) :?> Application
        application.Run ()
