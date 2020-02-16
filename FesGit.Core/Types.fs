namespace FesGit.Core

module Types =
    open System

    type Repository =
        { id: Guid
          name: string
          path: string
          createdAt: DateTime }
