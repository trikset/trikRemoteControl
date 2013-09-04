namespace TrikRemoteControl
open System

module Constants =
    [<Literal>]
    let ConnectionTimeout = 2000

    [<Literal>]
    let SendTimeout = 2000

/// Network client. Can connect to some endpoint and send string data.
type Client () =

    let mutable connectionState = None
    let syncContext = System.Threading.SynchronizationContext.Current
    
    let connectedEvent = Event<_> ()
    let connectionFailedEvent = Event<_> ()
    let disconnectedEvent = Event<_> ()    
    let postEvent (event : Event<_>) = syncContext.Post (System.Threading.SendOrPostCallback (fun _ -> event.Trigger ()), null)

    member this.ConnectedEvent = connectedEvent.Publish
    member this.ConnectionFailedEvent = connectionFailedEvent.Publish
    member this.DisconnectedEvent = disconnectedEvent.Publish
    
    member this.Connect (host : string, port: int) =
        this.Disconnect()
        async {
            let client = new System.Net.Sockets.TcpClient()
            let! _ = Async.AwaitIAsyncResult (client.ConnectAsync(host, port), Constants.ConnectionTimeout)
            if client.Connected then
                postEvent connectedEvent
                let keepAliveTimer = new System.Timers.Timer (1000.0, AutoReset = true)
                keepAliveTimer.Elapsed.Add (fun _ -> this.Send "keepalive")
                keepAliveTimer.Start ()
                connectionState <-  Some (new System.IO.StreamWriter (client.GetStream (), AutoFlush = true), keepAliveTimer, client)                
            else
                postEvent connectionFailedEvent
        } |> Async.Start

    member this.Send (str : string) = 
           connectionState |> Option.iter (fun (writer, timer, client) ->
               async {
                   do! Async.AwaitIAsyncResult (writer.WriteLineAsync str, Constants.SendTimeout) |> Async.Ignore
                   if not client.Connected then
                       this.Disconnect()
                } |> Async.Start)
                    
    member this.Disconnect () = 
        match connectionState with
          | None -> ()
          | Some (writer, timer, client) -> 
              writer.Close ()
              timer.Dispose ()
              client.Close ()
              connectionState <- None
              postEvent disconnectedEvent
        
    member this.Dispose () = this.Disconnect()

    interface System.IDisposable with
        member this.Dispose () = this.Dispose ()
