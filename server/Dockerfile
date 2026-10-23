# Stage 1: compile the TypeScript module to a single build/index.js
FROM node:24-alpine AS node-builder
WORKDIR /backend
COPY package*.json ./
RUN npm ci
COPY tsconfig.json ./
COPY src ./src
RUN npx tsc

# Stage 2: Nakama with the compiled module, the shared game data and config.
# "gamedata" is an extra build context (docker-compose.yml points it at ../shared/gamedata);
# the module reads the files with nk.fileRead("gamedata/<file>").
FROM heroiclabs/nakama:3.41.0
COPY --from=node-builder /backend/build/*.js /nakama/data/modules/build/
COPY --from=gamedata *.json /nakama/data/modules/gamedata/
COPY --from=gamedata navgrid/*.json /nakama/data/modules/gamedata/navgrid/
COPY local.yml /nakama/data/
