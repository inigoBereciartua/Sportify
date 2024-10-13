<template>
    <div>
        <div v-if="playlist == null">
            <h1 v-if="username">Welcome, {{ username }}!</h1>
            <p v-else>Fetching user info...</p>  <!-- Fallback text if username is empty -->

            <div class="form-container">
                <form @submit.prevent="submitForm">
                    <div class="form-group">
                        <label for="height">Height:</label>
                        <input type="number" v-model="height" id="height" />
                    </div>
                    <div class="form-group">
                        <label for="pace">Pace:</label>
                        <input type="number" step="0.1" v-model="pace" id="pace" />
                    </div>
                    <div class="form-group">
                        <label for="distance">Distance:</label>
                        <input type="number" step="0.1" v-model="distance" id="distance" />
                    </div>
                    <div class="submit-container">
                        <button type="submit">Submit</button>
                    </div>
                </form>
            </div>
        </div>
    </div>
</template>

<script>
import { useToast } from "vue-toastification";

export default {
    name: 'PlaylistInfoForm',
    data() {
        return {
            username: '',
            recentTracks: [],
            recentTracksWithBpm: [],
            playlist: null,
            selectedSongsIds: [],
            /* Form fields */
            height: 170,
            pace: 5.0,
            distance: 5.0
        };
    },
    mounted() {
        this.getUser();
    },
    methods: {
        async getUser() {
            const toast = useToast();
            try {
                const response = await fetch('http://localhost:5000/spotify/userinfo', {
                    credentials: 'include'
                });

                if (response.ok) {
                    const rawData = await response.text();

                    let data;
                    try {
                        data = JSON.parse(rawData);

                        if (typeof data === 'string') {
                            // For some reason the data is double-encoded
                            data = JSON.parse(data);
                        }
                        
                        if (data && data.username) {
                            this.username = data.id;
                        } else {
                            toast.error('An error has occured while processing user information request');
                        }
                    } catch (error) {
                        toast.error('An error has occured while processing user information request');
                        return;
                    }
                } else {
                    toast.error('An error has occured while processing user information request');
                }
            } catch (error) {
                toast.error('An error has occured while processing user information request');
            }
        },
        submitForm() {
            // Go to PlaylistPreview passing form data
            this.$router.push({
                name: 'PlaylistPreview',
                query: {
                    height: this.height,
                    pace: this.pace,
                    distance: this.distance
                }
            });
        },
    }
};
</script>

<style scoped>
.spotify-container {
    max-width: 900px;
    margin: 0 auto;
    padding: 20px;
    font-family: Arial, sans-serif;
}

.recent-tracks {
    margin-top: 20px;
}

.track-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); /* Responsive grid */
  gap: 20px;
}

.track-item {
  text-align: center;
  border: 1px solid #333;
  border-radius: 10px;
  padding: 10px;
  color: white;
  transition: transform 0.2s ease;
  cursor: pointer;
}

.track-item:hover {
  transform: scale(1.05); /* Slight zoom on hover */
}

.album-cover {
  width: 100px;
  height: 100px;
  object-fit: cover;
  margin-bottom: 10px;
  border-radius: 8px;
}

.track-name {
  font-size: 14px;
  font-weight: bold;
  margin: 5px 0;
  color: white;
}

.artist-name {
  font-size: 12px;
  color: grey;
}

.selected-song {
  border-color: #306844;
  border-width: 3px;
  border-style: solid;
}

progress {
  width: 100%;
  height: 20px;
  border-radius: 10px;
  overflow: hidden;
}

progress::-webkit-progress-bar {
  background-color: #182c25;
  border-radius: 10px;
}

progress::-webkit-progress-value {
    background-color: #1db954;
    border-radius: 10px;
}

.submit-container {
  text-align: center;
  margin-top: 20px;
}


.form-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 20px;
  background-color: #212121;
  border-radius: 10px;
  color: white;
}

.form-group {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  margin-bottom: 15px;
  width: 100%;
}

.form-group label {
  margin-bottom: 5px;
}

.form-group input {
  width: 100%;
  padding: 8px;
  border: 1px solid #455b55;
  border-radius: 5px;
}

.playlist-data {
  background-color: #212121;
  padding: 20px;
  border-radius: 10px;
}

</style>
