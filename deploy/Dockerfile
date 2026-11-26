# One parametrised Dockerfile for every .NET service in the repo.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /src
COPY Directory.Build.props ./
COPY src ./src
RUN dotnet publish ${PROJECT} -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG ASSEMBLY
ENV ASSEMBLY=${ASSEMBLY}
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "dotnet ${ASSEMBLY}.dll"]
