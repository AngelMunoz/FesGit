namespace FesGit

/// This is the main module of your application
/// here you handle all of your child pages as well as their
/// messages and their updates, useful to update multiple parts
/// of your application, Please refer to the `view` function
/// to see how to handle different kinds of "*child*" controls
module Shell =
    open Elmish
    open Avalonia
    open Avalonia.Controls
    open Avalonia.Input
    open Avalonia.FuncUI
    open Avalonia.FuncUI.Builder
    open Avalonia.FuncUI.Components.Hosts
    open Avalonia.FuncUI.DSL
    open Avalonia.FuncUI.Elmish
    open System
    open System.IO
    open FesGit.Core

    type State =
        { window: HostWindow
          aboutState: About.State
          repoState: Repositories.State }

    type Msg =
        | OpenFolder
        | OpenRepoWindow
        | HideMainWindow
        | ShowMainWindow
        | AfterOpenFolder of string option
        | AboutMsg of About.Msg
        | RepoMsg of Repositories.Msg

    let init window =
        let aboutState, bpCmd = About.init
        { window = window
          aboutState = aboutState
          repoState = Repositories.init },
        /// If your children controls don't emit any commands
        /// in the init function, you can just return Cmd.none
        /// otherwise, you can use a batch operation on all of them
        /// you can add more init commands as you need
        Cmd.batch [ bpCmd ]

    let private handleRepoExternal msg =
        match msg with
        | Some extrn ->
            match extrn with
            | Repositories.ExternalMsg.OpenFolder -> Cmd.ofMsg OpenFolder
            | Repositories.ExternalMsg.OpenRepoWindow -> Cmd.ofMsg OpenRepoWindow
        | None -> Cmd.none


    let update (msg: Msg) (state: State): State * Cmd<_> =
        match msg with
        | AboutMsg bpmsg ->
            let aboutState, cmd = About.update bpmsg state.aboutState
            { state with aboutState = aboutState },
            /// map the message to the kind of message
            /// your child control needs to handle
            Cmd.map AboutMsg cmd
        | RepoMsg rpmsg ->
            let repostate, cmd, external = Repositories.update rpmsg state.repoState

            let mapped = Cmd.map RepoMsg cmd
            let handled = handleRepoExternal external
            { state with repoState = repostate }, Cmd.batch [ mapped; handled ]
        | OpenFolder ->
            let operation() =
                async {
                    let! folder = Dialogs.asyncSelectFolder state.window
                    let result =
                        match isNull folder with
                        | true -> None
                        | false -> Some folder
                    return result
                }
            state, Cmd.OfAsync.perform operation () AfterOpenFolder
        | AfterOpenFolder folder ->
            match folder with
            | Some folder ->
                let name = folder.Split(Path.DirectorySeparatorChar) |> Array.last

                let repo: Types.Repository =
                    { id = Guid.NewGuid()
                      name = name
                      path = folder
                      createdAt = DateTime.Now }

                let newrepos = repo :: state.repoState.repos
                state, Cmd.ofMsg (RepoMsg(Repositories.Msg.SetRepositories(newrepos)))
            | None -> state, Cmd.none
        | OpenRepoWindow ->
            match state.repoState.selectedRepo with
            | Some repo ->
                let repoWindow = RepositoryDetails.RepositoryDetailsWindow(repo)
                repoWindow.Show()
                let sub dispatch = repoWindow.Closing.Subscribe(fun _ -> dispatch ShowMainWindow) |> ignore
                state,
                Cmd.batch
                    [ Cmd.ofMsg HideMainWindow
                      Cmd.ofSub sub ]
            | None -> state, Cmd.none
        | HideMainWindow ->
            state.window.Hide()
            state, Cmd.none
        | ShowMainWindow ->
            state.window.Show()
            state, Cmd.none


    let view (state: State) (dispatch) =
        DockPanel.create
            [ DockPanel.children
                [ TabControl.create
                    [ TabControl.tabStripPlacement Dock.Top
                      TabControl.viewItems
                          [ TabItem.create
                              [ TabItem.header "Repositories"
                                TabItem.content (Repositories.view state.repoState (RepoMsg >> dispatch)) ]
                            TabItem.create
                                [ TabItem.header "About"
                                  TabItem.content (About.view state.aboutState (AboutMsg >> dispatch)) ] ] ] ] ]

    type MainWindow() as this =
        inherit HostWindow()
        do
            base.Title <- "FesGit"
            base.Width <- 800.0
            base.Height <- 600.0
            base.MinWidth <- 800.0
            base.MinHeight <- 600.0

            //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
            //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true

            Elmish.Program.mkProgram init update view
            |> Program.withHost this
            |> Program.runWith this
