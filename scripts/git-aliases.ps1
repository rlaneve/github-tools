# Git functions+aliases
# I suck at PS, so this can probably be done muuuuuch better.
# Copy into your PS profile folder and then add the following line to your profile:
# . .\git-functions.ps1
# Use ". $PROFILE" to reload your profile and these functions+aliases will be active.

function git-status {
	git status
}
Set-Alias -Name gs -Value git-status

function git-log {
	git log --graph --pretty=format:'%Cred%h%Creset %an: %s - %Creset %C(yellow)%d%Creset %Cgreen(%cr)%Creset'
}
Set-Alias -Name glog -Value git-log

function git-branchshare {
	$branch_to_publish = $args[0]
	if($branch_to_publish -eq "") {
		return
	}
	git push origin $branch_to_publish
	git fetch origin
	git config branch.$branch_to_publish.remote origin
	git config branch.$branch_to_publish.merge refs/heads/$branch_to_publish
	git checkout $branch_to_publish
}
Set-Alias -Name gbshare -Value git-branchshare

function git-branchtrack {
	$branch_to_track = $args[0]
	if($branch_to_track -eq "") {
		return
	}
	git fetch origin
	git branch --track $branch_to_track origin/$branch_to_track
	
}
Set-Alias -Name gbtrack -Value git-branchtrack

function git-branchdelete {
	$branch_to_delete = $args[0]
	if($branch_to_delete -eq "") {
		return
	}
	git push origin :refs/heads/$branch_to_delete
	git branch -d $branch_to_delete
	
}
Set-Alias -Name gbdelete -Value git-branchdelete

function git-undo {
	git reset --soft HEAD^
}
Set-Alias -Name gundo -Value git-undo

function git-checkout {
	git checkout $args
}
Set-Alias -Name gco -Value git-checkout
