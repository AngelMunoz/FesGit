namespace FesGit

open Avalonia.FuncUI.Components
open Avalonia.Layout
open LibGit2Sharp


module Repositories =
    open FesGit.Core
    open Avalonia.Controls
    open Avalonia.FuncUI.DSL
    open Elmish

    type State =
        { repos: Types.Repository list
          selectedRepo: Types.Repository option
          selectedIndex: int }

    type ExternalMsg =
        | OpenFolder
        | OpenRepoWindow

    type Msg =
        | OpenFolder
        | SetIndex of int
        | SetRepository of Types.Repository option
        | OpenRepository of Types.Repository option
        | SetRepositories of Types.Repository list

    let init =
        { repos = List.empty
          selectedRepo = None
          selectedIndex = 0 }

    let update (msg: Msg) (state: State) =
        match msg with
        | SetRepository repo -> { state with selectedRepo = repo }, Cmd.none, None
        | SetIndex index -> { state with selectedIndex = index }, Cmd.none, None
        | OpenRepository repo -> { state with selectedRepo = repo }, Cmd.none, Some OpenRepoWindow
        | SetRepositories repos -> { state with repos = repos }, Cmd.none, None
        | OpenFolder -> state, Cmd.none, Some ExternalMsg.OpenFolder

    let private repoItemTemplate (repo: Types.Repository) dispatch =
        StackPanel.create
            [ StackPanel.spacing 12.0
              StackPanel.orientation Orientation.Vertical
              StackPanel.onDoubleTapped (fun _ -> dispatch (OpenRepository(Some repo)))
              StackPanel.children
                  [ TextBlock.create
                      [ TextBlock.classes [ "repotitle" ]
                        TextBlock.text repo.name ]
                    TextBlock.create
                        [ TextBlock.classes [ "repopath" ]
                          TextBlock.text repo.path ] ] ]

    let private repoList (state: State) (dispatch: Msg -> unit) =
        StackPanel.create
            [ StackPanel.dock Dock.Left
              StackPanel.spacing 12.0
              StackPanel.children
                  [ Button.create
                      [ Button.content "Add New Repo"
                        Button.onClick (fun _ -> dispatch OpenFolder) ]
                    ListBox.create
                        [ ListBox.dataItems state.repos
                          ListBox.selectedIndex state.selectedIndex
                          ListBox.onSelectedIndexChanged (fun idx -> dispatch (SetIndex idx))
                          ListBox.onSelectedItemChanged (fun repo ->
                              let repo =
                                  let optionable = repo |> Option.ofObj
                                  match optionable with
                                  | Some opt -> Some(opt :?> Types.Repository)
                                  | None -> None
                              dispatch (SetRepository(repo)))
                          ListBox.itemTemplate
                              (DataTemplateView<Types.Repository>.create(fun repo -> repoItemTemplate repo dispatch)) ] ] ]

    let private commitItemTemplate (commit: Commit) =
        StackPanel.create
            [ StackPanel.orientation Orientation.Vertical
              StackPanel.spacing 11.0
              StackPanel.children
                  [ StackPanel.create
                      [ StackPanel.spacing 18.0
                        StackPanel.children [ TextBlock.create [ TextBlock.text commit.Sha ] ] ]
                    StackPanel.create
                        [ StackPanel.spacing 18.0
                          StackPanel.children
                              [ TextBlock.create [ TextBlock.text commit.MessageShort ]
                                TextBlock.create
                                    [ TextBlock.text (sprintf "%s - %s" commit.Author.Name commit.Author.Email) ] ] ] ] ]

    let private selectedRepoTemplate (repo: Types.Repository) dispatch =
        StackPanel.create
            [ StackPanel.dock Dock.Top
              StackPanel.children
                  [ ListBox.create
                      [ ListBox.maxHeight 420.0
                        ListBox.dataItems (Git.listCommits repo)
                        ListBox.itemTemplate (DataTemplateView<Commit>.create commitItemTemplate) ] ] ]

    let private unSelectedRepoTemplate dispatch = DockPanel.create []

    let view (state: State) (dispatch: Msg -> unit) =
        DockPanel.create
            [ DockPanel.children
                [ repoList state dispatch
                  match state.selectedRepo with
                  | Some repo -> selectedRepoTemplate repo dispatch
                  | None -> unSelectedRepoTemplate dispatch ] ]
