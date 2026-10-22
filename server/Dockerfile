# Stage 1: compile the TypeScript module to a single build/index.js
FROM node:24-alpine AS node-builder
WORKDIR /backend
COPY package*.json ./
RUN npm ci
COPY tsconfig.json ./
COPY src ./src
RUN npx tsc

# Stage 2: Nakama with the compiled module and config
FROM heroiclabs/nakama:3.41.0
COPY --from=node-builder /backend/build/*.js /nakama/data/modules/build/
COPY local.yml /nakama/data/
