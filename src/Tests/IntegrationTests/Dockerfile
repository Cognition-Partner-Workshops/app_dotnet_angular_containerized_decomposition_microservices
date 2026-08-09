FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src
COPY . .
RUN chmod +x Tests/IntegrationTests/run-tests.sh
RUN dotnet restore Tests/IntegrationTests/IntegrationTests.csproj

ENV ORDER_URL=http://order-service:5003
ENV PRODUCT_URL=http://product-service:5004
ENV NOTIFICATION_URL=http://notification-service:5005

ENTRYPOINT ["Tests/IntegrationTests/run-tests.sh"]
