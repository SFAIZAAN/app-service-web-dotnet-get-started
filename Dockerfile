FROM mcr.microsoft.com/dotnet/framework/sdk:4.8.1-windowsservercore-ltsc2022 AS build
WORKDIR /src
COPY . .
RUN nuget restore "sf rental app.sln" -NonInteractive
RUN msbuild "sf rental app.sln" /p:Configuration=Release /verbosity:minimal

FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8.1-windowsservercore-ltsc2022
WORKDIR /inetpub/wwwroot
COPY --from=build /src/aspnet-get-started/bin/ ./bin/
COPY --from=build /src/aspnet-get-started/Content/ ./Content/
COPY --from=build /src/aspnet-get-started/fonts/ ./fonts/
COPY --from=build /src/aspnet-get-started/Scripts/ ./Scripts/
COPY --from=build /src/aspnet-get-started/Views/ ./Views/
COPY --from=build /src/aspnet-get-started/Global.asax ./
COPY --from=build /src/aspnet-get-started/Web.config ./
COPY --from=build /src/aspnet-get-started/favicon.ico ./

EXPOSE 80
