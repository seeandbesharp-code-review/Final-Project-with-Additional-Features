# Build stage
FROM node:18-alpine AS build
WORKDIR /app

COPY ChineseRaffleNg/package*.json ./
RUN npm ci

COPY ChineseRaffleNg/. .
RUN npm run build

# Serve stage
FROM node:18-alpine
WORKDIR /app
RUN npm install -g http-server

COPY --from=build /app/dist/chinese-raffle-ng ./dist

EXPOSE 4200
CMD ["http-server", "dist", "-p", "4200", "--cors", "-g"]
