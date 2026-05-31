FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY electrostoreWORKER.csproj .
RUN dotnet restore

COPY DTO/ DTO/
COPY Extensions/ Extensions/
COPY Grpc/ Grpc/
COPY Kafka/ Kafka/
COPY Mqtt/ Mqtt/
COPY Services/ Services/
COPY Program.cs ./

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

WORKDIR /app

RUN apk add --no-cache gcompat patchelf binutils

COPY --from=build /app/out .

# fix kafka native library dependencies for amr and x64 architectures
RUN if [ "$(uname -m)" = "aarch64" ]; then \
        patchelf --remove-needed ld-linux-aarch64.so.1 /app/runtimes/linux-arm64/native/librdkafka.so && \
        patchelf --add-needed libgcompat.so.0 /app/runtimes/linux-arm64/native/librdkafka.so; \
    elif [ "$(uname -m)" = "x86_64" ]; then \
        patchelf --remove-needed ld-linux-x86-64.so.2 /app/runtimes/linux-x64/native/librdkafka.so && \
        patchelf --add-needed libgcompat.so.0 /app/runtimes/linux-x64/native/librdkafka.so; \
    fi

EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget -qO- http://127.0.0.1:5000/health || exit 1

ENTRYPOINT ["dotnet", "electrostoreWORKER.dll"]
