FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY electrostoreAPI.csproj .
RUN dotnet restore

COPY Controllers/ Controllers/
COPY DTO/ DTO/
COPY Enums/ Enums/
COPY Extensions/ Extensions/
COPY Middleware/ Middleware/
COPY Migrations/ Migrations/
COPY Models/ Models/
COPY Grpc/ Grpc/
COPY Kafka/ Kafka/
COPY Services/ Services/
COPY Validators/ Validators/
COPY ApplicationDbContext.cs Mapper.cs Program.cs SwaggerClass.cs ./

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

#COPY entrypoint.sh /entrypoint.sh
#RUN chmod +x /entrypoint.sh && chown -R 3216:3216 /app

#USER 3216:3216

EXPOSE 5000
EXPOSE 5001

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget -qO- http://127.0.0.1:5000/health || exit 1

CMD ["dotnet", "electrostoreAPI.dll"]
