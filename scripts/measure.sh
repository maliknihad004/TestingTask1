#!/bin/bash

URL="http://localhost:5001/api/vehicles?brand=Chery&minPrice=80000&maxPrice=150000&year=2025&page=1&pageSize=10&sortBy=price&sortOrder=asc"

echo "First request (seconds):"
curl -s -o /dev/null -w '%{time_total}\n' "$URL"

echo "Average of next 20 requests (seconds):"
for i in $(seq 1 20)
do
    curl -s -o /dev/null -w '%{time_total}\n' "$URL"
done | awk '{sum+=$1} END {print sum/NR}'
