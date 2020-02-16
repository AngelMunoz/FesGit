namespace FesGit

open Avalonia.FuncUI.Components
open Avalonia.Layout
open Avalonia.Media
open System



module RepositoryDetails =
    open Elmish
    open Avalonia.Controls
    open Avalonia.FuncUI.Components.Hosts
    open Avalonia.FuncUI.DSL
    open Avalonia.FuncUI.Elmish
    open FesGit.Core
    open LibGit2Sharp


    type State =
        { repoRecord: Types.Repository
          repo: Repository
          status: RepositoryStatus
          selectedEntry: StatusEntry option
          selectedPatch: string option }

    type Msg =
        | GetStatus
        | SelectEntry of StatusEntry option


    let init (record: Types.Repository, repo: Repository) =
        let status = repo.RetrieveStatus()
        { repoRecord = record
          repo = repo
          status = status
          selectedEntry = None
          selectedPatch = None }, Cmd.none

    let update msg state =
        match msg with
        | GetStatus -> { state with status = state.repo.RetrieveStatus() }, Cmd.none
        | SelectEntry entry ->
            match entry with
            | Some entry ->
                use patch = state.repo.Diff.Compare<Patch>([| entry.FilePath |])
                printf "%A" patch.Content
                { state with
                      selectedEntry = Some entry
                      selectedPatch = Some patch.Content }, Cmd.none
            | None -> state, Cmd.none



    let private menubar state dispatch =
        Menu.create
            [ Menu.dock Dock.Top
              Menu.viewItems
                  [ MenuItem.create [ MenuItem.header "Pull" ]
                    MenuItem.create [ MenuItem.header "Push" ]
                    MenuItem.create [ MenuItem.header "Fetch" ] ] ]

    let private addedChangeItemTemplate (change: StatusEntry) dispatch =
        StackPanel.create
            [ StackPanel.spacing 12.0
              StackPanel.background "green"
              StackPanel.orientation Orientation.Horizontal
              StackPanel.children
                  [ TextBlock.create
                      [ TextBlock.text change.FilePath
                        TextBlock.foreground "white" ] ] ]

    let private removedChangeItemTemplate (change: StatusEntry) dispatch =
        StackPanel.create
            [ StackPanel.spacing 12.0
              StackPanel.background "red"
              StackPanel.orientation Orientation.Horizontal
              StackPanel.children
                  [ TextBlock.create
                      [ TextBlock.text change.FilePath
                        TextBlock.foreground "white" ] ] ]

    let private modifiedChangeItemTemplate (change: StatusEntry) dispatch =
        StackPanel.create
            [ StackPanel.spacing 12.0
              StackPanel.background "yellow"
              StackPanel.orientation Orientation.Horizontal
              StackPanel.children
                  [ TextBlock.create
                      [ TextBlock.text change.FilePath
                        TextBlock.foreground "Black" ] ] ]

    let private conflictedItemChange (change: StatusEntry) dispatch =
        StackPanel.create
            [ StackPanel.spacing 12.0
              StackPanel.background "orange"
              StackPanel.orientation Orientation.Horizontal
              StackPanel.children
                  [ TextBlock.create
                      [ TextBlock.text change.FilePath
                        TextBlock.foreground "black" ] ] ]

    let private changesDock state dispatch =
        let items =
            let added = state.status.Added |> Seq.toList
            let modified = state.status.Modified |> Seq.toList
            let removed = state.status.Removed |> Seq.toList
            let missing = state.status.Missing |> Seq.toList
            added @ modified @ removed @ missing

        let itemTemplate (change: StatusEntry) dispatch =
            match change.State with
            | FileStatus.NewInWorkdir -> addedChangeItemTemplate change dispatch
            | FileStatus.DeletedFromWorkdir | FileStatus.Nonexistent -> removedChangeItemTemplate change dispatch
            | FileStatus.Conflicted -> conflictedItemChange change dispatch
            | FileStatus.ModifiedInWorkdir -> modifiedChangeItemTemplate change dispatch
            | _ -> StackPanel.create []


        StackPanel.create
            [ StackPanel.dock Dock.Left
              StackPanel.children
                  [ ListBox.create
                      [ ListBox.dataItems items
                        ListBox.onSelectedItemChanged (fun entry ->
                            let entryopts = entry :?> StatusEntry |> Option.ofObj
                            dispatch (SelectEntry entryopts))
                        ListBox.itemTemplate
                            (DataTemplateView<StatusEntry>.create(fun change -> itemTemplate change dispatch)) ] ] ]

    let private infoDock state dispatch =
        StackPanel.create
            [ StackPanel.dock Dock.Top
              StackPanel.children
                  [ match state.selectedPatch with
                    | Some patch -> TextBlock.create [ TextBlock.text patch ]
                    | None -> TextBlock.create [ TextBlock.text "Select from the changelist" ] ] ]


    let view state dispatch =
        DockPanel.create
            [ DockPanel.lastChildFill false
              DockPanel.children
                  [ menubar state dispatch
                    changesDock state dispatch
                    infoDock state dispatch ] ]


    type RepositoryDetailsWindow(repo: Types.Repository) as this =
        inherit HostWindow()

        let repository: Repository = new Repository(repo.path)

        do
            base.Title <- sprintf "%s - %s" repo.name repo.path
            base.Width <- 800.0
            base.Height <- 600.0
            base.MinWidth <- 800.0
            base.MinHeight <- 600.0

            this.Closing.Subscribe(fun _ -> repository.Dispose()) |> ignore

            Program.mkProgram init update view
            |> Program.withHost this
            |> Program.runWith (repo, repository)
