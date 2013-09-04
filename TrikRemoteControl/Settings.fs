namespace TrikRemoteControl

open System.Configuration

type TrikSettings () = 
  inherit ApplicationSettingsBase ()
  
  [<UserScopedSettingAttribute()>]
  member this.IpAddress
    with get () = this.Item "IpAddress" :?> string
    and set (value : string) = 
        this.Item "IpAddress" <- value
        this.Save ()
