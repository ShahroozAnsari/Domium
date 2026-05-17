# Publishing Packages

Domium is configured for CI builds and NuGet publishing through one GitHub Actions workflow.

## Package Metadata

Common package metadata lives in `Directory.Build.props`:

- authors
- license expression
- package tags
- symbol package generation
- XML documentation files
- README packaging
- deterministic builds

Each package project defines its own description. The package ID defaults to the project name, for example `Domium.Persistence.Dapper`.

## Local Package Build

```powershell
dotnet restore Domium.slnx
dotnet build Domium.slnx --configuration Release --no-restore
dotnet test Domium.slnx --configuration Release --no-build
dotnet pack Domium.slnx --configuration Release --no-build --output artifacts/packages
```

The output folder contains `.nupkg` and `.snupkg` files.

## GitHub Secrets

Create a NuGet API key on nuget.org with permission to push the Domium packages.

Add it to the repository as:

```text
NUGET_API_KEY
```

GitHub path:

```text
Repository Settings -> Secrets and variables -> Actions -> New repository secret
```

## Versioning

The CI/CD workflow supports two publish version sources:

1. Tag-based publishing:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

The workflow strips the leading `v` and publishes version `0.1.0`.

2. Manual workflow dispatch:

Run the `CI/CD` workflow manually, enable `publish`, and provide a version such as:

```text
0.1.1
```

## CI/CD Workflow

The single CI/CD workflow runs on pushes, pull requests, version tags, and manual dispatch.

For normal branch pushes and pull requests it will:

- restore
- build
- test
- pack CI package artifacts
- upload package artifacts

For `vX.Y.Z` tags, or manual runs with `publish` enabled, it will also:

- pack with the selected version
- push `.nupkg` files to NuGet
- push `.snupkg` symbol packages to NuGet

NuGet push uses `--skip-duplicate` so rerunning the workflow is safe when a package version already exists.

## Release Checklist

1. Update release notes or changelog if one is used.
2. Confirm `dotnet test Domium.slnx --configuration Release` passes locally.
3. Choose a semantic version.
4. Push a `vX.Y.Z` tag or run the CI/CD workflow manually with `publish` enabled.
5. Confirm packages appear on NuGet.
6. Create a GitHub release from the tag.

## Repository URL Metadata

The workflow passes:

```text
/p:RepositoryUrl=https://github.com/{owner}/{repo}
```

This replaces the placeholder project URL during CI package builds.
