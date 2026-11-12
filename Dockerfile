# JobRunner container image (built by .github/workflows/release.yml).
#
# The image is assembled from the prebuilt framework-dependent publish
# layout produced by CI (publish/container), so the build needs no NuGet
# credentials and no SDK inside the image layers.
#
# Configuration via environment variables:
#   ConnectionStrings__JobDatabase   SQL Server connection string
#   JOBRUNNER_PLUGIN_DIRECTORY      plugin folder (defaults to /app/Plugins)
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY publish/container/ .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "JobRunner.dll"]
