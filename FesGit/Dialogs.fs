namespace FesGit

module Dialogs =
    open System
    open Avalonia.Controls

    let asyncSelectFolder window =
        let dialog = OpenFolderDialog()
        dialog.Title <- "Select a Git Repository"
        dialog.Directory <- Environment.GetFolderPath Environment.SpecialFolder.UserProfile
        async { return! dialog.ShowAsync window |> Async.AwaitTask }
