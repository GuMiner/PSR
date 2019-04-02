echo Stopping PSR...
echo a | sexec root@178.62.0.195:22 -cmd="supervisorctl stop psr" > nul

echo Copying PSR and PSR Views...
(
echo a
echo cd /home/helium24/psrdrop
echo lcd C:\Users\Gustave\Desktop\Projects\PuzzleSolveR\PSRdrop
echo put -o PSR.dll
echo put -o PSR.Views.dll) | sftpc root@178.62.0.195:22 > nul

echo Starting PSR...
echo a | sexec root@178.62.0.195:22 -cmd="supervisorctl start psr" > nul
echo Done
exit 0