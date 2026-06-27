.PHONY: up down init build run-orders test

up:            ## start local infra
	docker compose up -d

down:          ## stop local infra
	docker compose down

init:          ## create the solution file and add all projects
	bash scripts/init-solution.sh

build:
	dotnet build FleetIQ.slnx

run-orders:    ## run the Orders API
	dotnet run --project src/Services/Orders/FleetIQ.Orders.Api

test:
	dotnet test FleetIQ.slnx
