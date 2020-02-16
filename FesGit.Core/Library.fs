namespace FesGit.Core

module Say =
    let hello name = sprintf "Hello, %s" name


module Git =
    open LibGit2Sharp
    open System.IO

    let listCommits (repo: Types.Repository) =
        let git = new Repository(repo.path)
        git.Commits |> Seq.toArray

    let repoStatus (repo: Types.Repository) =
        let git = new Repository(repo.path)

        git.RetrieveStatus()
