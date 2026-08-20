# Multi-stage Dockerfile for EduFlowAI.Api (.NET 10.0)

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all project definition files for layer caching
COPY ["EduFlowAI.Api/EduFlowAI.Api.csproj", "EduFlowAI.Api/"]
COPY ["EduFlowAI.Admission/EduFlowAI.Admission.csproj", "EduFlowAI.Admission/"]
COPY ["EduFlowAI.AI/EduFlowAI.AI.csproj", "EduFlowAI.AI/"]
COPY ["EduFlowAI.Communication/EduFlowAI.Communication.csproj", "EduFlowAI.Communication/"]
COPY ["EduFlowAI.Documents/EduFlowAI.Documents.csproj", "EduFlowAI.Documents/"]
COPY ["EduFlowAI.Identity/EduFlowAI.Identity.csproj", "EduFlowAI.Identity/"]
COPY ["EduFlowAI.Persistence/EduFlowAI.Persistence.csproj", "EduFlowAI.Persistence/"]
COPY ["EduFlowAI.Shared.Kernel/EduFlowAI.Shared.Kernel.csproj", "EduFlowAI.Shared.Kernel/"]
COPY ["EduFlowAI.Shared.Messaging.Contracts/EduFlowAI.Shared.Messaging.Contracts.csproj", "EduFlowAI.Shared.Messaging.Contracts/"]

RUN dotnet restore "EduFlowAI.Api/EduFlowAI.Api.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/EduFlowAI.Api"
RUN dotnet build "EduFlowAI.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EduFlowAI.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EduFlowAI.Api.dll"]
