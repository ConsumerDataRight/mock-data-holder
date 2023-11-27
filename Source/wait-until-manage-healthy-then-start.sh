
while true; do
    
    # Check if manage api is healthy
    if [ "$Industry" = "Energy" ]; then
        echo "wait-until-manage-healthy-then-start.sh -" $1 "- Waiting for https://localhost:8105/health to respond OK"
        wget --no-check-certificate --no-verbose --spider https://localhost:8105/health
    elif [ "$Industry" = "Banking" ]; then
        echo "wait-until-manage-healthy-then-start.sh -" $1 "- Waiting for https://localhost:8005/health to respond OK"
        wget --no-check-certificate --no-verbose --spider https://localhost:8005/health
    fi

    # If healthy then exit code will be 0, so exit loop
    if [ $? -eq 0 ]; then 
        break 
    fi

    # Otherwise wait for 5 seconds and try again
    echo "wait-until-manage-healthy-then-start.sh -" $1 "- sleeping for 5 seconds"
    sleep 5s

done

# Start
echo "wait-until-manage-healthy-then-start.sh -" $1 "- starting"
/usr/bin/dotnet $1 

exit
